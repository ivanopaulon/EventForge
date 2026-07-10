namespace EventForge.Server.Services.Business;

public interface IFidelityPointsBaseRateService
{
    Task<IEnumerable<FidelityPointsBaseRate>> GetAllAsync(CancellationToken ct = default);
    Task<FidelityPointsBaseRate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityPointsBaseRate> CreateAsync(FidelityPointsBaseRate baseRate, string currentUser, CancellationToken ct = default);
    Task<FidelityPointsBaseRate?> UpdateAsync(Guid id, FidelityPointsBaseRate baseRate, string currentUser, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default);
}
