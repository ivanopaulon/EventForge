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

    private record ChatExportCacheEntry(byte[] Bytes, string Format, int RecordCount, DateTime CreatedAt);
    private static string ChatExportCacheKey(Guid exportId) => $"chat_export_{exportId}";

    /// <summary>
    /// Exports chat history and messages in JSON or CSV format.
    /// Queries the DB synchronously (up to MaxRecords), caches the result for 24h.
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(typeof(ChatExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatExportResultDto>> ExportChatHistoryAsync(
        [FromBody] ChatExportRequestDto exportRequest,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation(
                "Starting chat export for tenant {TenantId} from {FromDate} to {ToDate} in {Format} format",
                exportRequest.TenantId, exportRequest.FromDate, exportRequest.ToDate, exportRequest.Format);

            var maxRecords = Math.Min(exportRequest.MaxRecords ?? 10_000, 100_000);
            var search = new MessageSearchDto
            {
                ChatId = exportRequest.ChatId,
                TenantId = exportRequest.TenantId,
                SenderId = exportRequest.UserId,
                FromDate = exportRequest.FromDate,
                ToDate = exportRequest.ToDate,
                SearchTerm = exportRequest.SearchTerm,
                IncludeDeleted = exportRequest.IncludeDeleted,
                PageNumber = 1,
                PageSize = maxRecords
            };

            var paged = await chatService.GetMessagesAsync(search, cancellationToken);
            var messages = paged.Items.ToList();

            var exportId = Guid.NewGuid();
            byte[] bytes;
            string format = exportRequest.Format.ToUpperInvariant();

            if (format == "CSV")
                bytes = BuildChatCsvExport(messages);
            else
            {
                bytes = BuildChatJsonExport(exportId, messages);
                format = "JSON";
            }

            memoryCache.Set(
                ChatExportCacheKey(exportId),
                new ChatExportCacheEntry(bytes, format, messages.Count, DateTime.UtcNow),
                new Microsoft.Extensions.Caching.Memory.MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                    Size = 1
                });

            var result = new ChatExportResultDto
            {
                ExportId = exportId,
                Status = "Completed",
                Format = format,
                ProgressPercentage = 100,
                RecordCount = messages.Count,
                FileSizeBytes = bytes.Length,
                StatusUrl = Url.Action(nameof(GetChatExportStatusAsync), new { exportId }),
                DownloadUrl = Url.Action(nameof(DownloadChatExportAsync), new { exportId }),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            logger.LogInformation("Chat export {ExportId} completed: {RecordCount} records, {Format}, {Bytes} bytes",
                exportId, messages.Count, format, bytes.Length);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while generating the chat export", ex);
        }
    }

    /// <summary>
    /// Gets the status of a chat export operation (cached for 24h).
    /// </summary>
    [HttpGet("export/{exportId:guid}/status")]
    [ProducesResponseType(typeof(ChatExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<ChatExportResultDto> GetChatExportStatusAsync([FromRoute] Guid exportId)
    {
        if (!memoryCache.TryGetValue(ChatExportCacheKey(exportId), out ChatExportCacheEntry? entry) || entry is null)
            return CreateNotFoundProblem("Export not found or has expired.");

        var result = new ChatExportResultDto
        {
            ExportId = exportId,
            Status = "Completed",
            Format = entry.Format,
            ProgressPercentage = 100,
            RecordCount = entry.RecordCount,
            FileSizeBytes = entry.Bytes.Length,
            DownloadUrl = Url.Action(nameof(DownloadChatExportAsync), new { exportId }),
            ExpiresAt = entry.CreatedAt.AddHours(24),
            CompletedAt = entry.CreatedAt,
            CreatedAt = entry.CreatedAt
        };
        return Ok(result);
    }

    /// <summary>
    /// Downloads a previously generated chat export file (cached for 24h).
    /// </summary>
    [HttpGet("export/{exportId:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult DownloadChatExportAsync([FromRoute] Guid exportId)
    {
        if (!memoryCache.TryGetValue(ChatExportCacheKey(exportId), out ChatExportCacheEntry? entry) || entry is null)
            return CreateNotFoundProblem("Export not found or has expired.");

        var (contentType, fileExt) = entry.Format == "CSV"
            ? ("text/csv", "csv")
            : ("application/json", "json");

        logger.LogInformation("Serving chat export file for {ExportId} ({Format}, {Bytes} bytes)", exportId, entry.Format, entry.Bytes.Length);
        return File(entry.Bytes, contentType, $"chat-export-{exportId}.{fileExt}");
    }

    private static byte[] BuildChatJsonExport(Guid exportId, IReadOnlyList<ChatMessageDto> messages)
    {
        var payload = new
        {
            exportId,
            generatedAt = DateTime.UtcNow,
            format = "JSON",
            recordCount = messages.Count,
            messages
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    private static byte[] BuildChatCsvExport(IReadOnlyList<ChatMessageDto> messages)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,ChatId,SenderId,SenderName,Content,SentAt,Status,IsEdited,IsDeleted");
        foreach (var m in messages)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(m.Id.ToString()),
                CsvEscape(m.ChatId.ToString()),
                CsvEscape(m.SenderId.ToString()),
                CsvEscape(m.SenderName),
                CsvEscape(m.Content),
                CsvEscape(m.SentAt.ToString("O")),
                CsvEscape(m.Status.ToString()),
                m.IsEdited.ToString().ToLowerInvariant(),
                m.IsDeleted.ToString().ToLowerInvariant()));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string? value)
    {
        if (value is null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

}
