using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.SuperAdmin;

/// <summary>
/// DTO for user impersonation request.
/// </summary>
public class ImpersonateUserDto
{
    [Required]
    public Guid UserId { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    public Guid? TargetTenantId { get; set; }
}

/// <summary>
/// DTO for tenant switching request.
/// </summary>
public class SwitchTenantDto
{
    [Required]
    public Guid TenantId { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for ending impersonation.
/// </summary>
public class EndImpersonationDto
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for user management operations.
/// </summary>
public class UserManagementDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public List<string> Roles { get; set; } = new();
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// DTO for updating user status.
/// </summary>
public class UpdateUserStatusDto
{
    [Required]
    public bool IsActive { get; set; }
    
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for updating user roles.
/// </summary>
public class UpdateUserRolesDto
{
    [Required]
    public List<string> Roles { get; set; } = new();
    
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for forcing password change.
/// </summary>
public class ForcePasswordChangeDto
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for backup operation request.
/// </summary>
public class BackupRequestDto
{
    public bool IncludeAuditLogs { get; set; } = true;
    public bool IncludeUserData { get; set; } = true;
    public bool IncludeConfiguration { get; set; } = true;
    
    [MaxLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// DTO for backup operation status.
/// </summary>
public class BackupStatusDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ProgressPercentage { get; set; }
    public string? CurrentOperation { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public string StartedBy { get; set; } = string.Empty;
}

/// <summary>
/// DTO for audit log export request.
/// </summary>
public class AuditLogExportDto
{
    public string Format { get; set; } = "JSON"; // JSON, CSV, TXT
    
    public DateTime? FromDate { get; set; }
    
    public DateTime? ToDate { get; set; }
    
    public List<string>? OperationTypes { get; set; }
    
    public Guid? UserId { get; set; }
    
    public Guid? TenantId { get; set; }
    
    public bool? WasSuccessful { get; set; }
}