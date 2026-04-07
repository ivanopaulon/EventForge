namespace EventForge.DTOs.Common
{
    /// <summary>
    /// Constants for validation rules used across DTOs.
    /// </summary>
    public static class ValidationConstants
    {
        /// <summary>
        /// Minimum allowed quantity value for decimal quantity fields.
        /// This prevents zero or negative quantities while allowing very small fractional values.
        /// </summary>
        public const double MinimumQuantity = 0.0001;

        /// <summary>
        /// Maximum allowed quantity value for decimal quantity fields.
        /// </summary>
        public const double MaximumQuantity = 10000;
    }
}
