using System;
using System.Collections.Generic;

namespace EventForge.DTOs.PriceLists
{

    /// <summary>
    /// DTO representing the result of price list precedence validation.
    /// Part of Issue #245 price optimization implementation.
    /// </summary>
    public class PrecedenceValidationResultDto
    {
        /// <summary>
        /// Event identifier that was validated.
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Whether the validation passed without any issues.
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Date when the validation was performed.
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Total number of price lists validated.
        /// </summary>
        public int TotalPriceListsValidated { get; set; }

        /// <summary>
        /// Number of active price lists found.
        /// </summary>
        public int ActivePriceListsCount { get; set; }

        /// <summary>
        /// Number of default price lists found.
        /// </summary>
        public int DefaultPriceListsCount { get; set; }

        /// <summary>
        /// Number of expired price lists found.
        /// </summary>
        public int ExpiredPriceListsCount { get; set; }

        /// <summary>
        /// Collection of validation issues found.
        /// </summary>
        public ICollection<PrecedenceValidationIssueDto> Issues { get; set; } = new List<PrecedenceValidationIssueDto>();

        /// <summary>
        /// Collection of warnings about potential issues.
        /// </summary>
        public ICollection<PrecedenceValidationWarningDto> Warnings { get; set; } = new List<PrecedenceValidationWarningDto>();

        /// <summary>
        /// Summary of the validation result.
        /// </summary>
        public string Summary => IsValid
            ? $"Validation passed: {ActivePriceListsCount} active price lists with proper precedence"
            : $"Validation failed: {Issues.Count} issues found across {ActivePriceListsCount} active price lists";

        /// <summary>
        /// Recommended default price list based on precedence rules.
        /// </summary>
        public Guid? RecommendedDefaultPriceListId { get; set; }

        /// <summary>
        /// Name of the recommended default price list.
        /// </summary>
        public string? RecommendedDefaultPriceListName { get; set; }

        /// <summary>
        /// Performance metrics for the validation operation.
        /// </summary>
        public TimeSpan ValidationDuration { get; set; }
    }

    /// <summary>
    /// DTO representing a validation issue found during precedence validation.
    /// </summary>
    public class PrecedenceValidationIssueDto
    {
        /// <summary>
        /// Type of validation issue.
        /// </summary>
        public PrecedenceIssueType IssueType { get; set; }

        /// <summary>
        /// Severity level of the issue.
        /// </summary>
        public ValidationSeverity Severity { get; set; }

        /// <summary>
        /// Detailed description of the issue.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Price list identifier(s) involved in the issue.
        /// </summary>
        public ICollection<Guid> AffectedPriceListIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Price list names for reference.
        /// </summary>
        public ICollection<string> AffectedPriceListNames { get; set; } = new List<string>();

        /// <summary>
        /// Suggested resolution for the issue.
        /// </summary>
        public string? SuggestedResolution { get; set; }

        /// <summary>
        /// Impact of the issue on price calculation.
        /// </summary>
        public string? Impact { get; set; }
    }

    /// <summary>
    /// DTO representing a validation warning found during precedence validation.
    /// </summary>
    public class PrecedenceValidationWarningDto
    {
        /// <summary>
        /// Type of validation warning.
        /// </summary>
        public PrecedenceWarningType WarningType { get; set; }

        /// <summary>
        /// Description of the warning.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Price list identifier(s) involved in the warning.
        /// </summary>
        public ICollection<Guid> AffectedPriceListIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Recommendation to address the warning.
        /// </summary>
        public string? Recommendation { get; set; }
    }

    /// <summary>
    /// Enumeration of precedence validation issue types.
    /// </summary>
    public enum PrecedenceIssueType
    {
        NoPriceListsFound,
        MultipleDefaultPriceLists,
        NoDefaultPriceList,
        ConflictingPriorities,
        OverlappingValidityPeriods,
        ExpiredPriceListsOnly,
        CircularPrecedence,
        MissingValidityDates,
        InvalidPriorityValues
    }

    /// <summary>
    /// Enumeration of validation severity levels.
    /// </summary>
    public enum ValidationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Enumeration of precedence validation warning types.
    /// </summary>
    public enum PrecedenceWarningType
    {
        SoonToExpire,
        DuplicatePriorities,
        LargeValidityGaps,
        UnusualPriorityRange,
        ManyActivePriceLists,
        NoRecentUpdates
    }
}