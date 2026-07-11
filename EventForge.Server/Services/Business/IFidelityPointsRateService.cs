namespace EventForge.Server.Services.Business;

/// <summary>
/// Service for resolving the effective fidelity points accrual rate for a tenant.
/// </summary>
public interface IFidelityPointsRateService
{
    /// <summary>
    /// Gets the effective points rate (points per unit of currency spent) for the given
    /// fidelity tier, combining the tenant's base rate with the tier multiplier.
    /// </summary>
    /// <param name="tierId">Fidelity tier (level) identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Effective rate to multiply by the order total and the rounding mode to apply.</returns>
    Task<(decimal Rate, FidelityPointsRoundingMode Rounding)> GetEffectiveRateAsync(Guid tierId, CancellationToken cancellationToken = default);
}
