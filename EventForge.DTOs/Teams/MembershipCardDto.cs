namespace EventForge.DTOs.Teams
{
    /// <summary>
    /// DTO for MembershipCard output/display operations.
    /// </summary>
    public class MembershipCardDto
    {
        /// <summary>
        /// Unique identifier for the membership card.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Associated team member ID.
        /// </summary>
        public Guid TeamMemberId { get; set; }

        /// <summary>
        /// Team member name (for display purposes).
        /// </summary>
        public string? TeamMemberName { get; set; }

        /// <summary>
        /// Membership card number.
        /// </summary>
        public string CardNumber { get; set; } = string.Empty;

        /// <summary>
        /// Federation or organization that issued the card.
        /// </summary>
        public string Federation { get; set; } = string.Empty;

        /// <summary>
        /// Date from which the membership is valid.
        /// </summary>
        public DateTime ValidFrom { get; set; }

        /// <summary>
        /// Date until which the membership is valid.
        /// </summary>
        public DateTime ValidTo { get; set; }

        /// <summary>
        /// Associated document reference ID (if card document is uploaded).
        /// </summary>
        public Guid? DocumentReferenceId { get; set; }

        /// <summary>
        /// Category or type of membership (e.g., "Youth", "Senior", "Professional").
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Additional notes about the membership card.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the membership card was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the membership card.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the membership card was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the membership card.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Indicates if the membership is currently valid.
        /// </summary>
        public bool IsValid => DateTime.UtcNow >= ValidFrom && DateTime.UtcNow <= ValidTo;

        /// <summary>
        /// Days until expiration (calculated property).
        /// </summary>
        public int DaysUntilExpiration => (ValidTo.Date - DateTime.UtcNow.Date).Days;

        /// <summary>
        /// Status description based on validity.
        /// </summary>
        public string StatusDescription
        {
            get
            {
                if (!IsValid && DateTime.UtcNow < ValidFrom)
                    return "Not yet valid";
                if (!IsValid && DateTime.UtcNow > ValidTo)
                    return "Expired";
                if (DaysUntilExpiration <= 30 && DaysUntilExpiration > 0)
                    return "Expiring soon";
                return "Valid";
            }
        }
    }
}