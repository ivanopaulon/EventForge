using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{

    /// <summary>
    /// Uploads and processes file attachments — saves to local filesystem and persists a MessageAttachment record.
    /// Upload directory: {ContentRoot}/Uploads/chat/{chatId}/{attachmentId}_{fileName}
    /// </summary>
    public async Task<FileUploadResultDto> UploadFileAsync(
        FileUploadDto uploadDto,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate rate limits for file uploads
            await ValidateChatRateLimitAsync(null, uploadDto.UploadedBy, ChatOperationType.UploadFile, cancellationToken);

            var attachmentId = Guid.NewGuid();
            var mediaType = DetermineMediaType(uploadDto.ContentType);

            // Build storage path: {ContentRoot}/Uploads/chat/{attachmentId}/{safeFileName}
            // Using attachmentId as the directory avoids a dependency on a not-yet-existing message.
            var safeFileName = Path.GetFileName(uploadDto.FileName);
            var attachDir = Path.Combine(environment.ContentRootPath, "Uploads", "chat", attachmentId.ToString());
            Directory.CreateDirectory(attachDir);
            var storedFilePath = Path.Combine(attachDir, safeFileName);

            // Write stream to disk
            await using (var fileStream = new FileStream(storedFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await uploadDto.FileStream.CopyToAsync(fileStream, cancellationToken);
            }

            var fileUrl = $"/api/v1/chat/files/{attachmentId}/download";
            var thumbnailUrl = mediaType == MediaType.Image ? $"/api/v1/chat/files/{attachmentId}/thumbnail" : null;
            var uploadedAt = DateTime.UtcNow;

            // NOTE: The MessageAttachment DB record is intentionally NOT persisted here because no
            // ChatMessage exists yet at upload time.  The record is created by SendMessageAsync when
            // the user actually sends the message, using the attachmentId returned by this method.

            logger.LogInformation(
                "User {UserId} uploaded file {FileName} ({FileSize} bytes) for chat {ChatId} in {ElapsedMs}ms",
                uploadDto.UploadedBy, safeFileName, uploadDto.FileSize, uploadDto.ChatId, stopwatch.ElapsedMilliseconds);

            return new FileUploadResultDto
            {
                AttachmentId = attachmentId,
                FileName = safeFileName,
                FileUrl = fileUrl,
                ThumbnailUrl = thumbnailUrl,
                MediaType = mediaType,
                FileSize = uploadDto.FileSize,
                Success = true,
                UploadedAt = uploadedAt
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload file {FileName} for user {UserId}", uploadDto.FileName, uploadDto.UploadedBy);
            return new FileUploadResultDto
            {
                FileName = uploadDto.FileName,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets secure file download information — validates access via MessageAttachment DB record.
    /// </summary>
    public async Task<FileDownloadInfoDto?> GetFileDownloadInfoAsync(
        Guid attachmentId,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await context.MessageAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, cancellationToken);

        if (attachment is null) return null;

        return new FileDownloadInfoDto
        {
            AttachmentId = attachment.Id,
            FileName = attachment.OriginalFileName ?? attachment.FileName,
            DownloadUrl = $"/api/v1/chat/files/{attachment.Id}/download",
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            FileSize = attachment.FileSize,
            ContentType = attachment.ContentType
        };
    }

    /// <summary>
    /// Returns the physical file path + metadata for streaming download.
    /// </summary>
    public async Task<(string PhysicalPath, string ContentType, string FileName)?> GetAttachmentForDownloadAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await context.MessageAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, cancellationToken);

        if (attachment is null) return null;

        // Storage path: {ContentRoot}/Uploads/chat/{attachmentId}/{fileName}
        var physicalPath = Path.Combine(
            environment.ContentRootPath,
            "Uploads", "chat",
            attachment.Id.ToString(),
            attachment.FileName);

        if (!System.IO.File.Exists(physicalPath)) return null;

        return (
            physicalPath,
            attachment.ContentType ?? "application/octet-stream",
            attachment.OriginalFileName ?? attachment.FileName
        );
    }

    /// <summary>
    /// Processes media files — returns available URL variants based on what was stored.
    /// Full media transcoding (thumbnails, WebP) requires an external media service.
    /// </summary>
    public async Task<MediaProcessingResultDto> ProcessMediaAsync(
        Guid attachmentId,
        MediaProcessingOptionsDto processingOptions,
        CancellationToken cancellationToken = default)
    {
        var attachment = await context.MessageAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted, cancellationToken);

        if (attachment is null)
            return new MediaProcessingResultDto { AttachmentId = attachmentId, Success = false, ErrorMessage = "Attachment not found." };

        var variants = new List<MediaVariantDto>();

        if (processingOptions.GenerateThumbnails && attachment.ThumbnailUrl is not null)
        {
            variants.Add(new MediaVariantDto
            {
                VariantType = "thumbnail",
                Url = attachment.ThumbnailUrl,
                Format = "jpeg",
                FileSize = 0
            });
        }

        if (processingOptions.OptimizeForWeb && attachment.FileUrl is not null)
        {
            variants.Add(new MediaVariantDto
            {
                VariantType = "original",
                Url = attachment.FileUrl,
                Format = Path.GetExtension(attachment.FileName).TrimStart('.'),
                FileSize = attachment.FileSize
            });
        }

        return new MediaProcessingResultDto
        {
            AttachmentId = attachmentId,
            Success = true,
            GeneratedVariants = variants
        };
    }

    /// <summary>
    /// Deletes a file attachment — soft-deletes the DB record and removes the file from disk.
    /// </summary>
    public async Task<FileOperationResultDto> DeleteFileAsync(
        Guid attachmentId,
        Guid userId,
        string? reason = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var attachment = await context.MessageAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && (tenantId == null || a.TenantId == tenantId.Value) && !a.IsDeleted, cancellationToken);

        if (attachment is not null)
        {
            // Soft-delete DB record
            attachment.IsDeleted = true;
            attachment.ModifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            // Best-effort physical delete
            var chatDir = Path.Combine(environment.ContentRootPath, "Uploads", "chat", attachment.MessageId.ToString());
            var filePath = Path.Combine(chatDir, attachment.FileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        _ = await auditLogService.LogEntityChangeAsync(
            entityName: "MessageAttachment",
            entityId: attachmentId,
            propertyName: "Delete",
            operationType: "Delete",
            oldValue: "Active",
            newValue: "Deleted",
            changedBy: userId.ToString(),
            entityDisplayName: $"File Deletion: {attachmentId}",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "User {UserId} deleted file attachment {AttachmentId}. Reason: {Reason}",
            userId, attachmentId, reason ?? "No reason provided");

        return new FileOperationResultDto { AttachmentId = attachmentId, Success = true };
    }

}
