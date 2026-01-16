using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

public interface IDocumentStatusService
{
    Task<DocumentHeaderDto?> ChangeStatusAsync(
        Guid documentId, 
        DocumentStatus newStatus, 
        string? reason = null,
        CancellationToken cancellationToken = default);
    
    Task<List<DocumentStatusHistoryDto>> GetStatusHistoryAsync(
        Guid documentId, 
        CancellationToken cancellationToken = default);
    
    Task<List<DocumentStatus>> GetAvailableTransitionsAsync(
        Guid documentId, 
        CancellationToken cancellationToken = default);
    
    Task<StateTransitionValidationResult> ValidateTransitionAsync(
        Guid documentId, 
        DocumentStatus newStatus,
        CancellationToken cancellationToken = default);
}
