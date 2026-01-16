using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Client.Services;

public interface IDocumentStatusService
{
    Task<DocumentHeaderDto?> ChangeStatusAsync(Guid documentId, DocumentStatus newStatus, string? reason = null);
    Task<List<DocumentStatusHistoryDto>?> GetStatusHistoryAsync(Guid documentId);
    Task<List<DocumentStatus>?> GetAvailableTransitionsAsync(Guid documentId);
}
