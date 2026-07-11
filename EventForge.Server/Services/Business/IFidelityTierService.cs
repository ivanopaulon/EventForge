namespace EventForge.Server.Services.Business;

using EventForge.Server.Data.Entities.Business;

public interface IFidelityTierService
{
    Task<IEnumerable<FidelityTier>> GetAllAsync(CancellationToken ct = default);
    Task<FidelityTier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityTier> CreateAsync(FidelityTier tier, decimal? minimumSpendThreshold, int evaluationPeriodMonths, string currentUser, CancellationToken ct = default);
    Task<FidelityTier?> UpdateAsync(Guid id, FidelityTier tier, decimal? minimumSpendThreshold, int evaluationPeriodMonths, string currentUser, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default);
}
