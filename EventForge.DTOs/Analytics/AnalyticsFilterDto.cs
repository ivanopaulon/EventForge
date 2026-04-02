namespace EventForge.DTOs.Analytics
{
    /// <summary>
    /// Filter parameters for analytics queries.
    /// </summary>
    public class AnalyticsFilterDto
    {
        /// <summary>
        /// Start date of the analytics period.
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// End date of the analytics period.
        /// </summary>
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Maximum number of top items to return.
        /// </summary>
        public int? Top { get; set; }

        /// <summary>
        /// Grouping period: "day", "week", "month", "year".
        /// </summary>
        public string? GroupBy { get; set; }
    }
}
