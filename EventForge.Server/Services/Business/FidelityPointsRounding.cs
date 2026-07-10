using EventForge.Server.Data.Entities.Business;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Shared rounding logic for converting a raw fidelity points value (order total * effective rate)
/// into the integer number of points to award, according to the configured rounding mode.
/// </summary>
public static class FidelityPointsRounding
{
    /// <summary>
    /// Applies the given rounding mode to a raw (fractional) points value.
    /// </summary>
    /// <param name="rawPoints">Raw points value (e.g. orderTotal * effective rate), before rounding.</param>
    /// <param name="roundingMode">Rounding mode to apply.</param>
    /// <returns>The integer number of points to award.</returns>
    public static int Apply(decimal rawPoints, FidelityPointsRoundingMode roundingMode) => roundingMode switch
    {
        FidelityPointsRoundingMode.Ceiling => (int)Math.Ceiling(rawPoints),
        FidelityPointsRoundingMode.Nearest => (int)Math.Round(rawPoints, MidpointRounding.AwayFromZero),
        _ => (int)Math.Floor(rawPoints)
    };
}
