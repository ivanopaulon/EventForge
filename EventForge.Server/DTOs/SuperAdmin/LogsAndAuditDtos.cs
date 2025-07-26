using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.SuperAdmin;

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
    public Dictionary<string, int> OperationsByType { get; set; } = new();
    public Dictionary<string, int> OperationsByUser { get; set; } = new();
    public Dictionary<string, int> OperationsByTenant { get; set; } = new();
    public List<AuditTrendDto> RecentTrends { get; set; } = new();
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
    public List<string> MonitoredLevels { get; set; } = new() { "Warning", "Error", "Critical" };
    public List<string> MonitoredSources { get; set; } = new();
    public int MaxLiveEntries { get; set; } = 100;
    public bool AlertOnCritical { get; set; } = true;
    public bool AlertOnErrors { get; set; } = false;
}