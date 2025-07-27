using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Common
{

    /// <summary>
    /// DTO for updating an existing reference.
    /// </summary>
    public class UpdateReferenceDto
    {
        /// <summary>
        /// First name of the reference person.
        /// </summary>
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the reference person.
        /// </summary>
        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Department or role of the reference person.
        /// </summary>
        [MaxLength(50, ErrorMessage = "Department cannot exceed 50 characters.")]
        public string? Department { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        [MaxLength(100, ErrorMessage = "Notes cannot exceed 100 characters.")]
        public string? Notes { get; set; }
    }
}
