namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Represents the reason for a stock adjustment.
    /// </summary>
    public enum StockAdjustmentReason
    {
        /// <summary>
        /// Quick correction with automatic notes.
        /// </summary>
        QuickCorrection,

        /// <summary>
        /// Manual correction by user.
        /// </summary>
        ManualCorrection,

        /// <summary>
        /// Physical inventory count.
        /// </summary>
        Inventory,

        /// <summary>
        /// Product damaged.
        /// </summary>
        Damage,

        /// <summary>
        /// Product lost or missing.
        /// </summary>
        Loss,

        /// <summary>
        /// Product found/recovered.
        /// </summary>
        Found,

        /// <summary>
        /// Product expired.
        /// </summary>
        Expiry,

        /// <summary>
        /// Quality control issue.
        /// </summary>
        Quality,

        /// <summary>
        /// Other reason.
        /// </summary>
        Other
    }
}
