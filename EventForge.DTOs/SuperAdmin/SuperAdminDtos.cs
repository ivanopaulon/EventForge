using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.SuperAdmin
{
    /// <summary>
    /// DTO for tenant statistics.
    /// </summary>
    public class TenantStatisticsDto
    {
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int InactiveTenants { get; set; }
        public int TotalUsers { get; set; }
        public int UsersLastMonth { get; set; }
        public int TenantsNearLimit { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for user management.
    /// </summary>
    public class UserManagementDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Guid? TenantId { get; set; }
        public string? TenantName { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// DTO for creating users.
    /// </summary>
    public class CreateUserManagementDto
    {
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public Guid TenantId { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO for updating users.
    /// </summary>
    public class UpdateUserManagementDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new List<string>();

        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for user statistics.
    /// </summary>
    public class UserStatisticsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int UsersThisMonth { get; set; }
        public int AdminUsers { get; set; }
        public int ManagerUsers { get; set; }
        public int RegularUsers { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for backup operations.
    /// </summary>
    public class CreateBackupDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IncludeFiles { get; set; } = true;
        public bool IncludeDatabase { get; set; } = true;
        public bool IncludeConfiguration { get; set; } = true;
    }

    /// <summary>
    /// DTO for backup operation response.
    /// </summary>
    public class BackupOperationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// DTO for backup status.
    /// </summary>
    public class BackupStatusDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public long? FileSizeBytes { get; set; }
    }

    /// <summary>
    /// DTO for backup list item.
    /// </summary>
    public class BackupListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO for tenant switching.
    /// </summary>
    public class SwitchTenantDto
    {
        [Required]
        public Guid TenantId { get; set; }
        
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for tenant switch response.
    /// </summary>
    public class TenantSwitchResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid NewTenantId { get; set; }
        public string NewTenantName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for user impersonation.
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
    /// DTO for impersonation response.
    /// </summary>
    public class ImpersonationResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
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
    /// DTO for current context.
    /// </summary>
    public class CurrentContextDto
    {
        public Guid? TenantId { get; set; }
        public string? TenantName { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public bool IsImpersonating { get; set; }
        public string? ImpersonatingUser { get; set; }
    }
}