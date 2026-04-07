using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Licensing
{
    /// <summary>
    /// Data transfer object for Tenant License information.
    /// </summary>
    public class TenantLicenseDto
    {
        /// <summary>
        /// Unique identifier for the tenant license.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Tenant ID this license is assigned to.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Tenant name for display purposes.
        /// </summary>
        public string TenantName { get; set; } = string.Empty;

        /// <summary>
        /// License ID assigned to the tenant.
        /// </summary>
        public Guid LicenseId { get; set; }

        /// <summary>
        /// License name for display purposes.
        /// </summary>
        public string LicenseName { get; set; } = string.Empty;

        /// <summary>
        /// License display name for better UX.
        /// </summary>
        public string LicenseDisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Date when the license becomes active (UTC).
        /// </summary>
        [Required]
        public DateTime StartsAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the license expires (UTC).
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Indicates if the license is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Number of API calls made this month.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int ApiCallsThisMonth { get; set; } = 0;

        /// <summary>
        /// Maximum API calls allowed per month for this license.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int MaxApiCallsPerMonth { get; set; } = 10000;

        /// <summary>
        /// Date when API call count was last reset (UTC).
        /// </summary>
        public DateTime ApiCallsResetAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indicates if the license is currently valid and active.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Date when the tenant license was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the tenant license.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Date when the tenant license was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the tenant license.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// License tier level.
        /// </summary>
        public int TierLevel { get; set; } = 1;

        /// <summary>
        /// Maximum users allowed for this license.
        /// </summary>
        public int MaxUsers { get; set; } = 100;

        /// <summary>
        /// Current number of users for this tenant.
        /// </summary>
        public int CurrentUserCount { get; set; } = 0;

        /// <summary>
        /// Features available with this license.
        /// </summary>
        public List<LicenseFeatureDto> AvailableFeatures { get; set; } = new List<LicenseFeatureDto>();
    }
}