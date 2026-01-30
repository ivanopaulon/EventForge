namespace EventForge.DTOs.Teams
{
    /// <summary>
    /// Result of team member eligibility validation.
    /// </summary>
    public class EligibilityValidationResult
    {
        /// <summary>
        /// Indicates if the team member is eligible.
        /// </summary>
        public bool IsEligible { get; set; }

        /// <summary>
        /// List of validation issues found.
        /// </summary>
        public List<EligibilityIssue> Issues { get; set; } = new List<EligibilityIssue>();

        /// <summary>
        /// List of warnings (non-blocking issues).
        /// </summary>
        public List<EligibilityIssue> Warnings { get; set; } = new List<EligibilityIssue>();

        /// <summary>
        /// Date when the validation was performed.
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional notes about the validation.
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Represents an eligibility issue or warning.
    /// </summary>
    public class EligibilityIssue
    {
        /// <summary>
        /// Type of issue.
        /// </summary>
        public EligibilityIssueType Type { get; set; }

        /// <summary>
        /// Severity level of the issue.
        /// </summary>
        public EligibilityIssueSeverity Severity { get; set; }

        /// <summary>
        /// Description of the issue.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Field or area related to the issue.
        /// </summary>
        public string? Field { get; set; }

        /// <summary>
        /// Suggested action to resolve the issue.
        /// </summary>
        public string? SuggestedAction { get; set; }

        /// <summary>
        /// Due date for resolving the issue (if applicable).
        /// </summary>
        public DateTime? DueDate { get; set; }
    }

    /// <summary>
    /// Types of eligibility issues.
    /// </summary>
    public enum EligibilityIssueType
    {
        MissingDocument,        // Required document is missing
        ExpiredDocument,        // Document has expired
        InvalidDocument,        // Document is invalid or corrupted
        MissingEmergencyContact,// Emergency contact required for minors
        MissingConsent,         // Missing parental or photo consent
        InvalidData,            // Invalid or incomplete data
        PolicyViolation,        // Violates team or federation policies
        AgeRestriction,         // Age-related restrictions
        Other                   // Other issues
    }

    /// <summary>
    /// Severity levels for eligibility issues.
    /// </summary>
    public enum EligibilityIssueSeverity
    {
        Info,           // Informational only
        Warning,        // Warning but not blocking
        Error,          // Error that blocks eligibility
        Critical        // Critical issue requiring immediate attention
    }
}