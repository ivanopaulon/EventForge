using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Common
{

    /// <summary>
    /// DTO for updating an existing address.
    /// </summary>
    public class UpdateAddressDto
    {
        /// <summary>
        /// Address type (Legal, Operational, Destination, etc.).
        /// </summary>
        public AddressType AddressType { get; set; } = AddressType.Operational;

        /// <summary>
        /// Street and street number.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Street cannot exceed 100 characters.")]
        public string? Street { get; set; }

        /// <summary>
        /// City.
        /// </summary>
        [MaxLength(50, ErrorMessage = "City cannot exceed 50 characters.")]
        public string? City { get; set; }

        /// <summary>
        /// ZIP code.
        /// </summary>
        [MaxLength(10, ErrorMessage = "ZIP code cannot exceed 10 characters.")]
        public string? ZipCode { get; set; }

        /// <summary>
        /// Province.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Province cannot exceed 50 characters.")]
        public string? Province { get; set; }

        /// <summary>
        /// Country.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Country cannot exceed 50 characters.")]
        public string? Country { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Notes cannot exceed 100 characters.")]
        public string? Notes { get; set; }
    }
}
