namespace EventForge.Server.Services.Business;

/// <summary>
/// Evaluates whether a fidelity card qualifies for an automatic tier upgrade based on the
/// customer's recent completed spend. Only performs immediate promotions (never demotions);
/// periodic demotion is handled by <c>FidelityTierReevaluationBackgroundService</c>.
/// </summary>
public interface IFidelityTierEvaluationService
{
    /// <summary>
    /// Re-evaluates the given card and promotes it to the highest tier whose spend rule is
    /// currently satisfied, if that tier is higher than the card's current tier.
    /// </summary>
    /// <returns>True when the card was promoted; otherwise false.</returns>
    Task<bool> EvaluateUpgradeAsync(Guid fidelityCardId, CancellationToken cancellationToken = default);
}
