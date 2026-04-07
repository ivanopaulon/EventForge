using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace Prym.Client.Services;

public interface IDocumentStatusService
{
    Task<DocumentHeaderDto?> ChangeStatusAsync(Guid documentId, DocumentStatus newStatus, string? reason = null);
    Task<List<DocumentStatusHistoryDto>?> GetStatusHistoryAsync(Guid documentId);
    Task<List<DocumentStatus>?> GetAvailableTransitionsAsync(Guid documentId);
}
