using EventForge.Server.Data.Entities.Business;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Service for resolving the effective fidelity points accrual rate for a tenant.
/// </summary>
public interface IFidelityPointsRateService
{
    /// <summary>
    /// Gets the effective points rate (points per unit of currency spent) for the given
    /// fidelity card type, combining the tenant's base rate with the tier multiplier.
    /// </summary>
    /// <param name="cardType">Fidelity card tier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Effective rate to multiply by the order total.</returns>
    Task<decimal> GetEffectiveRateAsync(FidelityCardType cardType, CancellationToken cancellationToken = default);
}
