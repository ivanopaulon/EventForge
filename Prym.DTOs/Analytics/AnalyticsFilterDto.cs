namespace Prym.DTOs.Analytics
{

    /// <summary>
    /// Filter parameters for analytics queries.
    /// </summary>
    public class AnalyticsFilterDto
    {
        /// <summary>Start of the date range (inclusive).</summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>End of the date range (inclusive).</summary>
        public DateTime? DateTo { get; set; }

        /// <summary>Number of top items to return.</summary>
        public int Top { get; set; } = 10;

        /// <summary>Grouping granularity: "day", "week", or "month" (default: "month").</summary>
        public string? GroupBy { get; set; } = "month";
    }
}
