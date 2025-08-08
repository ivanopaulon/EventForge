using System;

namespace EventForge.DTOs.Licensing
{
    /// <summary>
    /// Data transfer object for API usage information.
    /// </summary>
    public class ApiUsageDto
    {
        /// <summary>
        /// Tenant ID.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Number of API calls made this month.
        /// </summary>
        public int ApiCallsThisMonth { get; set; }

        /// <summary>
        /// Maximum API calls allowed per month.
        /// </summary>
        public int MaxApiCallsPerMonth { get; set; }

        /// <summary>
        /// Percentage of API limit used.
        /// </summary>
        public decimal UsagePercentage => MaxApiCallsPerMonth > 0
            ? Math.Round((decimal)ApiCallsThisMonth / MaxApiCallsPerMonth * 100, 2)
            : 0;

        /// <summary>
        /// Remaining API calls this month.
        /// </summary>
        public int RemainingApiCalls => Math.Max(0, MaxApiCallsPerMonth - ApiCallsThisMonth);

        /// <summary>
        /// Date when API call count was last reset.
        /// </summary>
        public DateTime ApiCallsResetAt { get; set; }

        /// <summary>
        /// Date when API call count will next reset.
        /// </summary>
        public DateTime NextResetDate => new DateTime(
            ApiCallsResetAt.Year,
            ApiCallsResetAt.Month,
            1).AddMonths(1);

        /// <summary>
        /// Indicates if API limit is exceeded.
        /// </summary>
        public bool IsLimitExceeded => ApiCallsThisMonth >= MaxApiCallsPerMonth;

        /// <summary>
        /// Indicates if approaching limit (90% or more used).
        /// </summary>
        public bool IsApproachingLimit => UsagePercentage >= 90;
    }
}