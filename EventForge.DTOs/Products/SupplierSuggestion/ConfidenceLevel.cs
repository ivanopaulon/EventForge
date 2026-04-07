namespace EventForge.DTOs.Products.SupplierSuggestion
{
    /// <summary>
    /// Confidence level for supplier recommendations.
    /// </summary>
    public enum ConfidenceLevel
    {
        /// <summary>
        /// Low confidence (score < 60).
        /// </summary>
        Low,

        /// <summary>
        /// Medium confidence (score 60-80).
        /// </summary>
        Medium,

        /// <summary>
        /// High confidence (score > 80).
        /// </summary>
        High
    }
}
