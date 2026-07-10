namespace EventForge.Server.Services.Business;

public interface IFidelityTierMultiplierService
{
    Task<IEnumerable<FidelityTierMultiplier>> GetAllAsync(CancellationToken ct = default);
    Task<FidelityTierMultiplier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityTierMultiplier> CreateAsync(FidelityTierMultiplier tierMultiplier, string currentUser, CancellationToken ct = default);
    Task<FidelityTierMultiplier?> UpdateAsync(Guid id, FidelityTierMultiplier tierMultiplier, string currentUser, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default);
}
