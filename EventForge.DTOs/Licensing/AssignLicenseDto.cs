using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Licensing
{
    /// <summary>
    /// Data transfer object for assigning a license to a tenant.
    /// </summary>
    public class AssignLicenseDto
    {
        /// <summary>
        /// Tenant ID to assign the license to.
        /// </summary>
        [Required]
        public Guid TenantId { get; set; }

        /// <summary>
        /// License ID to assign to the tenant.
        /// </summary>
        [Required]
        public Guid LicenseId { get; set; }

        /// <summary>
        /// Date when the license becomes active (UTC).
        /// </summary>
        public DateTime StartsAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the license expires (UTC). If null, license never expires.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Indicates if the license should be activated immediately.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}