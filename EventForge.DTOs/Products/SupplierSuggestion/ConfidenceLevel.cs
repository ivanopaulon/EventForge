namespace EventForge.DTOs.Products.SupplierSuggestion
{
    /// <summary>
    /// Confidence level for supplier recommendations.
    /// </summary>
    public enum ConfidenceLevel
    {
        /// <summary>
        /// Low confidence (score &lt; 60).
        /// </summary>
        Low,

        /// <summary>
        /// Medium confidence (score 60-80).
        /// </summary>
        Medium,

        /// <summary>
        /// High confidence (score &gt; 80).
        /// </summary>
        High
    }
}
