using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Licensing
{
    /// <summary>
    /// Data transfer object for License Feature information.
    /// </summary>
    public class LicenseFeatureDto
    {
        /// <summary>
        /// Unique identifier for the license feature.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Unique name of the feature.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable display name for the feature.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Description of what this feature provides.
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Category/module this feature belongs to.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this feature is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// License ID this feature belongs to.
        /// </summary>
        public Guid LicenseId { get; set; }

        /// <summary>
        /// License name for display purposes.
        /// </summary>
        public string LicenseName { get; set; } = string.Empty;

        /// <summary>
        /// Date when the feature was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the feature.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Date when the feature was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the feature.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Permissions required for this feature.
        /// </summary>
        public List<string> RequiredPermissions { get; set; } = new List<string>();
    }
}