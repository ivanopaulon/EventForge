using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.SuperAdmin;

/// <summary>
/// DTO for user search and filtering.
/// </summary>
public class UserSearchDto
{
    [MaxLength(100)]
    public string? SearchTerm { get; set; }
    
    public Guid? TenantId { get; set; }
    
    public string? Role { get; set; } // "all", "admin", "manager", "user"
    
    public bool? IsActive { get; set; }
    
    public bool? MustChangePassword { get; set; }
    
    public DateTime? LastLoginAfter { get; set; }
    
    public DateTime? LastLoginBefore { get; set; }
    
    public DateTime? CreatedAfter { get; set; }
    
    public DateTime? CreatedBefore { get; set; }
    
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 20;
    
    public string? SortBy { get; set; } = "CreatedAt";
    
    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// DTO for user role and permission information.
/// </summary>
public class UserRolePermissionDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<RoleInfoDto> Roles { get; set; } = new();
    public List<string> DirectPermissions { get; set; } = new();
    public List<string> EffectivePermissions { get; set; } = new();
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
}

/// <summary>
/// DTO for role information.
/// </summary>
public class RoleInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public bool IsSystemRole { get; set; }
    public DateTime AssignedAt { get; set; }
}

/// <summary>
/// DTO for quick user actions.
/// </summary>
public class QuickUserActionDto
{
    [Required]
    public string Action { get; set; } = string.Empty; // "enable", "disable", "resetPassword", "forcePasswordChange", "lockout", "unlock"
    
    [Required]
    public List<Guid> UserIds { get; set; } = new();
    
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// DTO for quick action result.
/// </summary>
public class QuickActionResultDto
{
    public string Action { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int SuccessfulActions { get; set; }
    public int FailedActions { get; set; }
    public List<UserActionResultDto> Results { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for individual user action result.
/// </summary>
public class UserActionResultDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Result { get; set; }
}

/// <summary>
/// DTO for user statistics.
/// </summary>
public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int UsersPendingPasswordChange { get; set; }
    public int LockedUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int LoginsToday { get; set; }
    public int FailedLoginsToday { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
    public Dictionary<string, int> UsersByTenant { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for creating a new user.
/// </summary>
public class CreateUserDto
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public Guid TenantId { get; set; }
    
    public List<string> Roles { get; set; } = new();
    
    public bool MustChangePassword { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for user creation result.
/// </summary>
public class UserCreationResultDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string GeneratedPassword { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public List<string> AssignedRoles { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}