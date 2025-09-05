using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Teams
{
    /// <summary>
    /// DTO for MembershipCard creation operations.
    /// </summary>
    public class CreateMembershipCardDto
    {
        /// <summary>
        /// Associated team member ID.
        /// </summary>
        [Required]
        [Display(Name = "Team Member", Description = "Associated team member.")]
        public Guid TeamMemberId { get; set; }

        /// <summary>
        /// Membership card number.
        /// </summary>
        [Required(ErrorMessage = "The card number is required.")]
        [MaxLength(50, ErrorMessage = "The card number cannot exceed 50 characters.")]
        [Display(Name = "Card Number", Description = "Membership card number.")]
        public string CardNumber { get; set; } = string.Empty;

        /// <summary>
        /// Federation or organization that issued the card.
        /// </summary>
        [Required(ErrorMessage = "The federation is required.")]
        [MaxLength(100, ErrorMessage = "The federation cannot exceed 100 characters.")]
        [Display(Name = "Federation", Description = "Federation or organization that issued the card.")]
        public string Federation { get; set; } = string.Empty;

        /// <summary>
        /// Date from which the membership is valid.
        /// </summary>
        [Required]
        [Display(Name = "Valid From", Description = "Date from which the membership is valid.")]
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// Date until which the membership is valid.
        /// </summary>
        [Required]
        [Display(Name = "Valid To", Description = "Date until which the membership is valid.")]
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// Associated document reference ID (if card document is uploaded).
        /// </summary>
        [Display(Name = "Document Reference", Description = "Associated document reference.")]
        public Guid? DocumentReferenceId { get; set; }

        /// <summary>
        /// Category or type of membership (e.g., "Youth", "Senior", "Professional").
        /// </summary>
        [MaxLength(50, ErrorMessage = "The category cannot exceed 50 characters.")]
        [Display(Name = "Category", Description = "Category or type of membership.")]
        public string? Category { get; set; }

        /// <summary>
        /// Additional notes about the membership card.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The notes cannot exceed 500 characters.")]
        [Display(Name = "Notes", Description = "Additional notes about the membership card.")]
        public string? Notes { get; set; }
    }
}