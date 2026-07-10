namespace EventForge.Server.Services.Business;

public interface IFidelityPointsCampaignService
{
    Task<IEnumerable<FidelityPointsCampaign>> GetAllAsync(CancellationToken ct = default);
    Task<FidelityPointsCampaign?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FidelityPointsCampaign> CreateAsync(FidelityPointsCampaign campaign, string currentUser, CancellationToken ct = default);
    Task<FidelityPointsCampaign?> UpdateAsync(Guid id, FidelityPointsCampaign campaign, string currentUser, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default);
}
