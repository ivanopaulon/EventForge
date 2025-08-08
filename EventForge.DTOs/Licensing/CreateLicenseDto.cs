using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Licensing
{
    /// <summary>
    /// Data transfer object for creating a new license.
    /// </summary>
    public class CreateLicenseDto
    {
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
        /// License tier level (1 = Basic, 2 = Standard, 3 = Premium, etc.).
        /// </summary>
        [Range(1, 10)]
        public int TierLevel { get; set; } = 1;

        /// <summary>
        /// Indicates if this license is active and can be assigned.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}