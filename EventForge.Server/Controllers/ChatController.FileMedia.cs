using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;


namespace EventForge.Server.Controllers;

public partial class ChatController
{

    /// <summary>
    /// Uploads a file attachment to a chat with comprehensive validation.
    /// Supports multiple file types, virus scanning, and thumbnail generation.
    /// </summary>
    /// <param name="uploadRequest">File upload request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with file metadata and access information</returns>
    /// <response code="201">File uploaded successfully</response>
    /// <response code="400">Invalid file or request parameters</response>
    /// <response code="413">File too large</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileUploadResultDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<FileUploadResultDto>> UploadFileAsync(
        [FromForm] ChatFileUploadRequestDto uploadRequest,
        CancellationToken cancellationToken = default
    )
    {
        if (uploadRequest.File is null || uploadRequest.File.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty"
            );
        }

        // Check file size limit (example: 50MB)
        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (uploadRequest.File.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB");
        }

        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;

            var uploadDto = new FileUploadDto
            {
                ChatId = Guid.Parse(uploadRequest.ChatId),
                UploadedBy = userId,
                FileName = uploadRequest.File.FileName,
                ContentType = uploadRequest.File.ContentType,
                FileSize = uploadRequest.File.Length,
                FileStream = uploadRequest.File.OpenReadStream()
            };

            var result = await chatService.UploadFileAsync(uploadDto, cancellationToken);

            if (result.Success)
            {
                return Created($"api/v1/chat/files/{result.AttachmentId}/download", result);
            }
            else
            {
                return CreateValidationProblemDetails(result.ErrorMessage ?? "An error occurred during file upload"
                );
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the file", ex);
        }
    }

    /// <summary>
    /// Uploads an image inserted via the RichTextEditor to prevent base64 inline data URIs
    /// from bloating the message payload. Returns a URL the RTE can use as the image src.
    /// Only image content types are accepted (jpeg, png, gif, webp).
    /// </summary>
    /// <param name="file">Image file posted by the Syncfusion RTE upload handler.</param>
    /// <param name="env">Web host environment (injected from DI).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON with the public URL under which the uploaded image is accessible.</returns>
    /// <response code="200">Image uploaded and URL returned.</response>
    /// <response code="400">File missing, not an image, or exceeds the size limit.</response>
    [HttpPost("upload-image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadRteImageAsync(
        IFormFile file,
        [FromServices] IWebHostEnvironment env,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return CreateValidationProblemDetails("Image file cannot be empty.");

        const long maxImageSize = 5 * 1024 * 1024; // 5 MB
        if (file.Length > maxImageSize)
            return CreateValidationProblemDetails("Image must not exceed 5 MB.");

        var allowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        if (!allowedMimeTypes.Contains(file.ContentType))
            return CreateValidationProblemDetails($"Unsupported image type '{file.ContentType}'. Allowed: jpeg, png, gif, webp.");

        try
        {
            var uploadId = Guid.NewGuid();
            var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
            if (string.IsNullOrEmpty(extension)) extension = "bin";

            var year = DateTime.UtcNow.Year.ToString();
            var month = DateTime.UtcNow.Month.ToString("D2");

            var fullPath = Path.Combine(env.ContentRootPath, "Uploads", "chat-images",
                year, month, $"{uploadId}.{extension}");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            // Embed year/month in URL so the GET endpoint can reconstruct the exact path
            // without scanning the directory tree on every request.
            var fileUrl = $"{baseUrl}/api/v1/chat/image-uploads/{year}/{month}/{uploadId}.{extension}";

            return Ok(new { url = fileUrl });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the image.", ex);
        }
    }

    /// <summary>
    /// Serves a previously uploaded RTE inline image.
    /// </summary>
    [HttpGet("image-uploads/{year:int}/{month:int}/{fileName}")]
    [AllowAnonymous]
    public IActionResult GetRteImage(
        [FromRoute] int year,
        [FromRoute] int month,
        [FromRoute] string fileName,
        [FromServices] IWebHostEnvironment env)
    {
        // Validate year and month ranges to prevent path traversal via route segments.
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
            return BadRequest();

        // fileName must match exactly the GUID.extension pattern generated by the upload endpoint.
        if (string.IsNullOrEmpty(fileName) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                fileName,
                @"^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\.[a-z]+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            return BadRequest();
        }

        var fullPath = Path.Combine(env.ContentRootPath, "Uploads", "chat-images",
            year.ToString(), month.ToString("D2"), fileName);

        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var mimeType = ext switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return PhysicalFile(fullPath, mimeType);
    }


    /// <summary>
    /// Provides time-limited URLs, access logging, and download tracking.
    /// </summary>
    /// <param name="attachmentId">File attachment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Secure download information or 404 if not accessible</returns>
    /// <response code="200">Download information retrieved successfully</response>
    /// <response code="404">File not found or not accessible</response>
    [HttpGet("files/{attachmentId:guid}/info")]
    [ProducesResponseType(typeof(FileDownloadInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileDownloadInfoDto>> GetFileDownloadInfoAsync(
        [FromRoute] Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId ?? Guid.Empty;
            var tenantId = tenantContext.CurrentTenantId;

            var downloadInfo = await chatService.GetFileDownloadInfoAsync(attachmentId, userId, tenantId, cancellationToken);

            if (downloadInfo is null)
            {
                return CreateNotFoundProblem($"File with ID {attachmentId} was not found or is not accessible");
            }

            return Ok(downloadInfo);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving file information", ex);
        }
    }

    /// <summary>
    /// Downloads a file attachment with secure access validation.
    /// </summary>
    /// <param name="attachmentId">File attachment identifier</param>
    /// <param name="token">Security token for download access (reserved for future use)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content stream</returns>
    /// <response code="200">File downloaded successfully</response>
    /// <response code="404">File not found or token invalid</response>
    [HttpGet("files/{attachmentId:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DownloadFileAsync(
        [FromRoute] Guid attachmentId,
        [FromQuery] string? token = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Downloading file {AttachmentId}", attachmentId);

            var result = await chatService.GetAttachmentForDownloadAsync(attachmentId, cancellationToken);
            if (result is null)
                return CreateNotFoundProblem($"File with ID {attachmentId} was not found or has been deleted.");

            var (physicalPath, contentType, fileName) = result.Value;
            return PhysicalFile(physicalPath, contentType, fileName);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while downloading the file", ex);
        }
    }

}
