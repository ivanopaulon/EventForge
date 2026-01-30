namespace EventForge.DTOs.Bulk;

/// <summary>
/// Specifies the type of price update operation.
/// </summary>
public enum PriceUpdateType
{
    /// <summary>
    /// Replace the current price with a new value.
    /// </summary>
    Replace = 0,

    /// <summary>
    /// Increase the current price by a percentage.
    /// </summary>
    IncreaseByPercentage = 1,

    /// <summary>
    /// Decrease the current price by a percentage.
    /// </summary>
    DecreaseByPercentage = 2,

    /// <summary>
    /// Increase the current price by a fixed amount.
    /// </summary>
    IncreaseByAmount = 3,

    /// <summary>
    /// Decrease the current price by a fixed amount.
    /// </summary>
    DecreaseByAmount = 4
}
