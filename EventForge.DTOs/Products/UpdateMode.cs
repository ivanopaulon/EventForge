namespace EventForge.DTOs.Products
{
    /// <summary>
    /// Defines the mode for bulk price updates.
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>
        /// Set the value directly (replace current value).
        /// </summary>
        Set,

        /// <summary>
        /// Increase by a fixed amount.
        /// </summary>
        Increase,

        /// <summary>
        /// Decrease by a fixed amount.
        /// </summary>
        Decrease,

        /// <summary>
        /// Increase by a percentage.
        /// </summary>
        PercentageIncrease,

        /// <summary>
        /// Decrease by a percentage.
        /// </summary>
        PercentageDecrease
    }
}
