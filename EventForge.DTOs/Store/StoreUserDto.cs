using EventForge.DTOs.Common;
using System;
namespace EventForge.DTOs.Store
{

    /// <summary>
    /// DTO for StoreUser output/display operations.
    /// </summary>
    public class StoreUserDto
    {
        /// <summary>
        /// Unique identifier for the store user.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Display name of the operator/cashier.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Username for login.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the operator.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Role or permissions of the operator.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Status of the operator.
        /// </summary>
        public CashierStatus Status { get; set; }

        /// <summary>
        /// Date and time of the last login.
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Additional notes about the operator.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Cashier group the operator belongs to.
        /// </summary>
        public Guid? CashierGroupId { get; set; }

        /// <summary>
        /// Cashier group name (for display purposes).
        /// </summary>
        public string? CashierGroupName { get; set; }

        // --- Issue #315: Image Management & Extended Fields ---

        /// <summary>
        /// Photo document identifier (references DocumentReference).
        /// </summary>
        public Guid? PhotoDocumentId { get; set; }

        /// <summary>
        /// Photo URL (from PhotoDocument if available).
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Photo thumbnail URL (from PhotoDocument if available).
        /// </summary>
        public string? PhotoThumbnailUrl { get; set; }

        /// <summary>
        /// Indicates if the operator has given consent for photo storage (GDPR compliance).
        /// </summary>
        public bool PhotoConsent { get; set; }

        /// <summary>
        /// Date and time when photo consent was given.
        /// </summary>
        public DateTime? PhotoConsentAt { get; set; }

        /// <summary>
        /// Phone number of the operator.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Date and time of the last password change.
        /// </summary>
        public DateTime? LastPasswordChangedAt { get; set; }

        /// <summary>
        /// Indicates if two-factor authentication is enabled.
        /// </summary>
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        /// External ID for integration with external authentication providers.
        /// </summary>
        public string? ExternalId { get; set; }

        /// <summary>
        /// Indicates if the operator is currently on shift.
        /// </summary>
        public bool IsOnShift { get; set; }

        /// <summary>
        /// Current shift identifier (if on shift).
        /// </summary>
        public Guid? ShiftId { get; set; }

        /// <summary>
        /// Date and time when the store user was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the store user.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the store user was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the store user.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
