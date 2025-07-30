using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.SuperAdmin
{

/// <summary>
/// Query parameters for filtering and paginating application logs.
/// </summary>
public class ApplicationLogQueryParameters
{
    /// <summary>
    /// Filter by log level.
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Filter by source/logger name.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Filter by message content.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Filter by user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Filter by tenant ID.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Filter by start date (inclusive).
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive).
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter logs with exceptions only.
    /// </summary>
    public bool? HasException { get; set; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field (defaults to Timestamp).
    /// </summary>
    public string SortBy { get; set; } = "Timestamp";

    /// <summary>
    /// Sort direction (asc or desc, defaults to desc).
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Calculate skip count for pagination.
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// DTO for system logs search and filtering.
/// </summary>
public class SystemLogSearchDto
{
    [MaxLength(100)]
    public string? SearchTerm { get; set; }

    public string? Level { get; set; } // "Trace", "Debug", "Information", "Warning", "Error", "Critical"

    public string? Source { get; set; }

    public string? Category { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }

    public string? SessionId { get; set; }

    public string? TraceId { get; set; }

    public bool? HasException { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? SortBy { get; set; } = "Timestamp";

    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// DTO for system log entry.
/// </summary>
public class SystemLogDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Source { get; set; }
    public Guid? UserId { get; set; }
    public string? Username { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? SessionId { get; set; }
    public string? TraceId { get; set; }
    public string? Exception { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// DTO for audit trail search and filtering.
/// </summary>
public class AuditTrailSearchDto
{
    [MaxLength(100)]
    public string? SearchTerm { get; set; }

    public List<string>? OperationTypes { get; set; }

    public Guid? UserId { get; set; }

    public Guid? SourceTenantId { get; set; }

    public Guid? TargetTenantId { get; set; }

    public Guid? TargetUserId { get; set; }

    public bool? WasSuccessful { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string? SessionId { get; set; }

    public string? IpAddress { get; set; }

    public bool? CriticalOperation { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? SortBy { get; set; } = "PerformedAt";

    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// DTO for audit trail statistics.
/// </summary>
public class AuditTrailStatisticsDto
{
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public int CriticalOperations { get; set; }
    public int OperationsToday { get; set; }
    public int OperationsThisWeek { get; set; }
    public int OperationsThisMonth { get; set; }
    public Dictionary<string, int> OperationsByType { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> OperationsByUser { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> OperationsByTenant { get; set; } = new Dictionary<string, int>();
    public List<AuditTrendDto> RecentTrends { get; set; } = new List<AuditTrendDto>();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for audit operation trends.
/// </summary>
public class AuditTrendDto
{
    public DateTime Date { get; set; }
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public int CriticalOperations { get; set; }
}

/// <summary>
/// DTO for audit trail response.
/// </summary>
public class AuditTrailResponseDto
{
    public Guid Id { get; set; }
    public AuditOperationType OperationType { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string PerformedByUsername { get; set; } = string.Empty;
    public Guid? SourceTenantId { get; set; }
    public string? SourceTenantName { get; set; }
    public Guid? TargetTenantId { get; set; }
    public string? TargetTenantName { get; set; }
    public Guid? TargetUserId { get; set; }
    public string? TargetUsername { get; set; }
    public bool WasSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool CriticalOperation { get; set; }
    public string? SessionId { get; set; }
}

/// <summary>
/// DTO for export request.
/// </summary>
public class ExportRequestDto
{
    [Required]
    public string Type { get; set; } = string.Empty; // "audit", "systemlogs", "users", "tenants"

    [Required]
    public string Format { get; set; } = "JSON"; // "JSON", "CSV", "EXCEL"

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public Dictionary<string, object>? Filters { get; set; }

    public List<string>? Columns { get; set; }

    public int? MaxRecords { get; set; } = 10000;

    [MaxLength(200)]
    public string? FileName { get; set; }
}

/// <summary>
/// DTO for export result.
/// </summary>
public class ExportResultDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Processing", "Completed", "Failed"
    public int? TotalRecords { get; set; }
    public string? FileName { get; set; }
    public string? DownloadUrl { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// DTO for real-time log monitoring configuration.
/// </summary>
public class LogMonitoringConfigDto
{
    public bool EnableRealTimeUpdates { get; set; } = true;
    public int UpdateIntervalSeconds { get; set; } = 5;
    public List<string> MonitoredLevels { get; set; } = new List<string>() { "Warning", "Error", "Critical" };
    public List<string> MonitoredSources { get; set; } = new List<string>();
    public int MaxLiveEntries { get; set; } = 100;
    public bool AlertOnCritical { get; set; } = true;
    public bool AlertOnErrors { get; set; } = false;
}

/// <summary>
/// DTO for security validation request.
/// </summary>
public class SecurityValidationDto
{
    [Required]
    public string ValidationCode { get; set; } = string.Empty;
    
    [Required]
    public string RequestType { get; set; } = string.Empty; // "tenant-switch", "impersonation", etc.
    
    public Guid? TargetTenantId { get; set; }
    
    public Guid? TargetUserId { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for security validation result.
/// </summary>
public class SecurityValidationResultDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    public int RemainingAttempts { get; set; }
    public DateTime? NextAttemptAllowedAt { get; set; }
}

/// <summary>
/// DTO for tenant switch history.
/// </summary>
public class TenantSwitchHistoryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid? SourceTenantId { get; set; }
    public string? SourceTenantName { get; set; }
    public Guid TargetTenantId { get; set; }
    public string TargetTenantName { get; set; } = string.Empty;
    public DateTime SwitchedAt { get; set; }
    public string? Reason { get; set; }
    public bool WasSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// DTO for operation history search criteria.
/// </summary>
public class OperationHistorySearchDto
{
    [MaxLength(100)]
    public string? SearchTerm { get; set; }
    
    public Guid? UserId { get; set; }
    
    public List<string>? OperationTypes { get; set; }
    
    public Guid? SourceTenantId { get; set; }
    
    public Guid? TargetTenantId { get; set; }
    
    public bool? WasSuccessful { get; set; }
    
    public DateTime? FromDate { get; set; }
    
    public DateTime? ToDate { get; set; }
    
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 20;
    
    public string? SortBy { get; set; } = "PerformedAt";
    
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// DTO for impersonation history.
/// </summary>
public class ImpersonationHistoryDto
{
    public Guid Id { get; set; }
    public Guid SuperAdminId { get; set; }
    public string SuperAdminUsername { get; set; } = string.Empty;
    public Guid TargetUserId { get; set; }
    public string TargetUsername { get; set; } = string.Empty;
    public Guid? TargetTenantId { get; set; }
    public string? TargetTenantName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// DTO for operation summary.
/// </summary>
public class OperationSummaryDto
{
    public int TotalOperations { get; set; }
    public int TenantSwitches { get; set; }
    public int ImpersonationSessions { get; set; }
    public int FailedOperations { get; set; }
    public int OperationsToday { get; set; }
    public int ActiveImpersonations { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, int> OperationsByType { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> OperationsByUser { get; set; } = new Dictionary<string, int>();
}

/// <summary>
/// DTO for tenant switch with audit information.
/// </summary>
public class TenantSwitchWithAuditDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid? SourceTenantId { get; set; }
    public string? SourceTenantName { get; set; }
    public Guid TargetTenantId { get; set; }
    public string TargetTenantName { get; set; } = string.Empty;
    public DateTime SwitchedAt { get; set; }
    public string? Reason { get; set; }
    public bool WasSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public AuditTrailResponseDto? AuditTrail { get; set; }
}

/// <summary>
/// DTO for impersonation with audit information.
/// </summary>
public class ImpersonationWithAuditDto
{
    public Guid Id { get; set; }
    public Guid SuperAdminId { get; set; }
    public string SuperAdminUsername { get; set; } = string.Empty;
    public Guid TargetUserId { get; set; }
    public string TargetUsername { get; set; } = string.Empty;
    public Guid? TargetTenantId { get; set; }
    public string? TargetTenantName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public AuditTrailResponseDto? AuditTrail { get; set; }
}

}
