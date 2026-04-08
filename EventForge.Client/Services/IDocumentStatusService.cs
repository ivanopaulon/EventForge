using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

public interface IDocumentStatusService
{
    Task<DocumentHeaderDto?> ChangeStatusAsync(Guid documentId, DocumentStatus newStatus, string? reason = null, CancellationToken ct = default);
    Task<List<DocumentStatusHistoryDto>?> GetStatusHistoryAsync(Guid documentId, CancellationToken ct = default);
    Task<List<DocumentStatus>?> GetAvailableTransitionsAsync(Guid documentId, CancellationToken ct = default);
}
