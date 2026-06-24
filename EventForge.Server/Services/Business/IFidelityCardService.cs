using Prym.DTOs.Business.Fidelity;

namespace EventForge.Server.Services.Business;

public interface IFidelityCardService
{
    Task<IEnumerable<FidelityCardDto>> GetAllCardsAsync(CancellationToken ct = default);
    Task<IEnumerable<FidelityCardDto>> GetCardsByBusinessPartyAsync(Guid businessPartyId, CancellationToken ct = default);
    Task<FidelityCardDto?> GetCardByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityCardDto> CreateCardAsync(CreateFidelityCardDto dto, string currentUser, CancellationToken ct = default);
    Task<FidelityCardDto?> UpdateCardAsync(Guid id, UpdateFidelityCardDto dto, string currentUser, CancellationToken ct = default);
    Task<bool> RevokeCardAsync(Guid id, string currentUser, CancellationToken ct = default);
    Task<bool> SuspendCardAsync(Guid id, string currentUser, CancellationToken ct = default);
    Task<bool> ActivateCardAsync(Guid id, string currentUser, CancellationToken ct = default);
    Task<FidelityPointsTransactionDto?> AddPointsAsync(Guid id, ModifyFidelityPointsDto dto, string currentUser, CancellationToken ct = default);
    Task<FidelityPointsTransactionDto?> RedeemPointsAsync(Guid id, ModifyFidelityPointsDto dto, string currentUser, CancellationToken ct = default);
    Task<IEnumerable<FidelityPointsTransactionDto>> GetTransactionHistoryAsync(Guid cardId, CancellationToken ct = default);
    Task<bool> DeleteCardAsync(Guid id, string currentUser, CancellationToken ct = default);
}
