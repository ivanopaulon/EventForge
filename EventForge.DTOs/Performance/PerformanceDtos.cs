using System;

namespace EventForge.DTOs.Performance
{
    /// <summary>
    /// Performance summary DTO for dashboard display.
    /// </summary>
    public class PerformanceSummaryDto
    {
        /// <summary>
        /// Total number of queries executed.
        /// </summary>
        public long TotalQueries { get; set; }

        /// <summary>
        /// Number of slow queries.
        /// </summary>
        public long SlowQueries { get; set; }

        /// <summary>
        /// Percentage of slow queries.
        /// </summary>
        public double SlowQueryPercentage { get; set; }

        /// <summary>
        /// Average query duration in milliseconds.
        /// </summary>
        public double AverageQueryDurationMs { get; set; }

        /// <summary>
        /// Slowest query duration in milliseconds.
        /// </summary>
        public double SlowestQueryDurationMs { get; set; }

        /// <summary>
        /// Number of recent slow queries in memory.
        /// </summary>
        public int RecentSlowQueryCount { get; set; }

        /// <summary>
        /// Timestamp of the last slow query.
        /// </summary>
        public DateTime? LastSlowQueryTime { get; set; }

        /// <summary>
        /// Overall performance level (Excellent, Good, Fair, Poor).
        /// </summary>
        public string PerformanceLevel { get; set; } = string.Empty;
    }
}