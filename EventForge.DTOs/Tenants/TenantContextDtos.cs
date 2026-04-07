using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Tenants
{
    /// <summary>
    /// Request DTO for tenant switching.
    /// </summary>
    public class SwitchTenantRequest
    {
        /// <summary>
        /// Target tenant ID to switch to.
        /// </summary>
        [Required(ErrorMessage = "Tenant ID is required.")]
        [Display(Name = "field.tenantId")]
        public Guid TenantId { get; set; }

        /// <summary>
        /// Reason for the tenant switch.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        [Display(Name = "field.reason")]
        public string Reason { get; set; } = "Tenant switch by admin";
    }

    /// <summary>
    /// Request DTO for starting user impersonation.
    /// </summary>
    public class StartImpersonationRequest
    {
        /// <summary>
        /// User ID to impersonate.
        /// </summary>
        [Required(ErrorMessage = "User ID is required.")]
        [Display(Name = "field.userId")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Reason for impersonation.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        [Display(Name = "field.reason")]
        public string Reason { get; set; } = "User impersonation by admin";
    }

    /// <summary>
    /// Request DTO for ending user impersonation.
    /// </summary>
    public class EndImpersonationRequest
    {
        /// <summary>
        /// Reason for ending impersonation.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        [Display(Name = "field.reason")]
        public string Reason { get; set; } = "Impersonation ended by admin";
    }
}