using System.Globalization;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Resolves the effective fidelity points accrual rate for the current tenant, reusing the
/// existing <c>SystemConfigurations</c> table (category "FidelityPoints").
/// </summary>
/// <remarks>
/// Unlike <see cref="Configuration.IConfigurationService"/>.GetValueAsync (which resolves a single,
/// tenant-agnostic value per key), this service reads configuration rows scoped to
/// <see cref="ITenantContext.CurrentTenantId"/> so that base rate and tier multipliers can be
/// customized per tenant, as required for fidelity points accrual.
/// </remarks>
public class FidelityPointsRateService(
    EventForgeDbContext context,
    ITenantContext tenantContext) : IFidelityPointsRateService
{
    private const string BaseRateKey = "FidelityPoints.BaseRate";
    private const string DefaultBaseRate = "1";
    private const string DefaultMultiplier = "1.0";

    public async Task<decimal> GetEffectiveRateAsync(FidelityCardType cardType, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.CurrentTenantId ?? Guid.Empty;
        var multiplierKey = $"FidelityPoints.Multiplier.{cardType}";

        var baseRate = await GetDecimalValueAsync(tenantId, BaseRateKey, DefaultBaseRate, cancellationToken);
        var multiplier = await GetDecimalValueAsync(tenantId, multiplierKey, DefaultMultiplier, cancellationToken);

        return baseRate * multiplier;
    }

    private async Task<decimal> GetDecimalValueAsync(Guid tenantId, string key, string defaultValue, CancellationToken cancellationToken)
    {
        var value = await context.SystemConfigurations
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.Key == key)
            .Select(c => c.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return decimal.Parse(defaultValue, CultureInfo.InvariantCulture);
    }
}
