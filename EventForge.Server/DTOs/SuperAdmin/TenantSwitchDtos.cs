using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.SuperAdmin;

/// <summary>
/// DTO for tenant switching history.
/// </summary>
public class TenantSwitchHistoryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public Guid? FromTenantId { get; set; }
    public string? FromTenantName { get; set; }
    public Guid ToTenantId { get; set; }
    public string ToTenantName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime SwitchedAt { get; set; }
    public DateTime? SwitchedBackAt { get; set; }
    public bool IsActive { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// DTO for user impersonation history.
/// </summary>
public class ImpersonationHistoryDto
{
    public Guid Id { get; set; }
    public Guid ImpersonatorUserId { get; set; }
    public string ImpersonatorUsername { get; set; } = string.Empty;
    public Guid ImpersonatedUserId { get; set; }
    public string ImpersonatedUsername { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? Reason { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
    public List<string> ActionsPerformed { get; set; } = new();
}

/// <summary>
/// DTO for current context information.
/// </summary>
public class CurrentContextDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Guid? CurrentTenantId { get; set; }
    public string? CurrentTenantName { get; set; }
    public Guid? OriginalTenantId { get; set; }
    public string? OriginalTenantName { get; set; }
    public bool IsImpersonating { get; set; }
    public Guid? ImpersonatedUserId { get; set; }
    public string? ImpersonatedUsername { get; set; }
    public bool IsSuperAdmin { get; set; }
    public string? SessionId { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivity { get; set; }
    public string? IpAddress { get; set; }
    public List<string> ActiveSessions { get; set; } = new();
}

/// <summary>
/// DTO for tenant switching with audit.
/// </summary>
public class TenantSwitchWithAuditDto : SwitchTenantDto
{
    public bool CreateAuditEntry { get; set; } = true;
    public string? AuditCategory { get; set; } = "TenantSwitch";
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// DTO for impersonation with audit.
/// </summary>
public class ImpersonationWithAuditDto : ImpersonateUserDto
{
    public bool CreateAuditEntry { get; set; } = true;
    public string? AuditCategory { get; set; } = "UserImpersonation";
    public Dictionary<string, object>? AdditionalData { get; set; }
    public List<string> AllowedActions { get; set; } = new(); // Optional: restrict what can be done during impersonation
    public DateTime? ExpiresAt { get; set; } // Optional: automatic expiration
}

/// <summary>
/// DTO for security validation request.
/// </summary>
public class SecurityValidationDto
{
    [Required]
    public string Action { get; set; } = string.Empty; // "switch", "impersonate", "endImpersonation"

    [Required]
    public Guid UserId { get; set; }

    public Guid? TargetUserId { get; set; }

    public Guid? TargetTenantId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public string? SecurityCode { get; set; } // For additional verification if required

    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// DTO for security validation result.
/// </summary>
public class SecurityValidationResultDto
{
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Requirements { get; set; } = new();
    public bool RequiresAdditionalConfirmation { get; set; }
    public Dictionary<string, object>? ValidationData { get; set; }
}

/// <summary>
/// DTO for operation history search.
/// </summary>
public class OperationHistorySearchDto
{
    public Guid? UserId { get; set; }

    public List<string>? OperationTypes { get; set; } // "switch", "impersonate", "admin_action"

    public Guid? TenantId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public bool? IsActive { get; set; }

    public string? SessionId { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string? SortBy { get; set; } = "StartedAt";

    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// DTO for operation summary statistics.
/// </summary>
public class OperationSummaryDto
{
    public int TotalTenantSwitches { get; set; }
    public int ActiveTenantSwitches { get; set; }
    public int TotalImpersonations { get; set; }
    public int ActiveImpersonations { get; set; }
    public int OperationsToday { get; set; }
    public int OperationsThisWeek { get; set; }
    public int OperationsThisMonth { get; set; }
    public Dictionary<string, int> OperationsByUser { get; set; } = new();
    public Dictionary<string, int> OperationsByTenant { get; set; } = new();
    public List<RecentOperationDto> RecentOperations { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for recent operations.
/// </summary>
public class RecentOperationDto
{
    public string Type { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? TargetUsername { get; set; }
    public string? TenantName { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}