using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Licensing
{
    /// <summary>
    /// Data transfer object for License information.
    /// </summary>
    public class LicenseDto
    {
        /// <summary>
        /// Unique identifier for the license.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Unique name of the license type.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable display name for the license.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this license includes.
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Maximum number of users allowed with this license.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int MaxUsers { get; set; } = 100;

        /// <summary>
        /// Maximum number of API calls per month allowed with this license.
        /// </summary>
        [Range(0, int.MaxValue)]
        public int MaxApiCallsPerMonth { get; set; } = 10000;

        /// <summary>
        /// Indicates if this license is active and can be assigned.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// License tier level (1 = Basic, 2 = Standard, 3 = Premium, etc.).
        /// </summary>
        [Range(1, 10)]
        public int TierLevel { get; set; } = 1;

        /// <summary>
        /// Date when the license was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the license.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Date when the license was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the license.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Features available with this license.
        /// </summary>
        public List<LicenseFeatureDto> Features { get; set; } = new List<LicenseFeatureDto>();

        /// <summary>
        /// Number of tenants currently using this license.
        /// </summary>
        public int TenantCount { get; set; }
    }
}