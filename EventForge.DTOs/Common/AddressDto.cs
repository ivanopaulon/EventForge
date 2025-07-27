using System;

namespace EventForge.DTOs.Common
{

    /// <summary>
    /// DTO for Address output/display operations.
    /// </summary>
    public class AddressDto
    {
        /// <summary>
        /// Unique identifier for the address.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the owning entity.
        /// </summary>
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Type of the owning entity.
        /// </summary>
        public string OwnerType { get; set; } = string.Empty;

        /// <summary>
        /// Address type.
        /// </summary>
        public AddressType AddressType { get; set; }

        /// <summary>
        /// Street and street number.
        /// </summary>
        public string? Street { get; set; }

        /// <summary>
        /// City.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// ZIP code.
        /// </summary>
        public string? ZipCode { get; set; }

        /// <summary>
        /// Province.
        /// </summary>
        public string? Province { get; set; }

        /// <summary>
        /// Country.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the address was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the address.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the address was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the address.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
