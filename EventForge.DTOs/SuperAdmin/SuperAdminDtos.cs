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
    /// DTO for updating user status.
    /// </summary>
    public class UpdateUserStatusDto
    {
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
        public List<string> Roles { get; set; } = new List<string>();
        
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
        
        public bool RequireChangeOnNextLogin { get; set; } = true;
    }

    /// <summary>
    /// DTO for user search criteria.
    /// </summary>
    public class UserSearchDto
    {
        [MaxLength(100)]
        public string? SearchTerm { get; set; }
        
        public Guid? TenantId { get; set; }
        
        public List<string>? Roles { get; set; }
        
        public bool? IsActive { get; set; }
        
        public DateTime? CreatedFrom { get; set; }
        
        public DateTime? CreatedTo { get; set; }
        
        public DateTime? LastLoginFrom { get; set; }
        
        public DateTime? LastLoginTo { get; set; }
        
        public int PageNumber { get; set; } = 1;
        
        public int PageSize { get; set; } = 20;
        
        public string? SortBy { get; set; } = "CreatedAt";
        
        public string? SortOrder { get; set; } = "desc";
    }

    /// <summary>
    /// DTO for quick user actions.
    /// </summary>
    public class QuickUserActionDto
    {
        [Required]
        public List<Guid> UserIds { get; set; } = new List<Guid>();
        
        [Required]
        public string Action { get; set; } = string.Empty; // "activate", "deactivate", "unlock", "force-password-change"
        
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for quick action results.
    /// </summary>
    public class QuickActionResultDto
    {
        public string Action { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int SuccessfulActions { get; set; }
        public int FailedActions { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
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

        [MaxLength(100)]
        public string? Phone { get; set; }

        public bool SendWelcomeEmail { get; set; } = true;
    }

    /// <summary>
    /// DTO for user creation result.
    /// </summary>
    public class UserCreationResultDto
    {
        public bool Success { get; set; }
        public Guid? UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TemporaryPassword { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<string> Warnings { get; set; } = new List<string>();
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
    /// DTO for backup request (simplified version for API calls).
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
        public string StartedByUserName { get; set; } = string.Empty;
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

    /// <summary>
    /// DTO for application log.
    /// </summary>
    public class ApplicationLogDto
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }
        public Guid? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? RequestId { get; set; }
        public string? RequestPath { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// DTO for application log statistics.
    /// </summary>
    public class ApplicationLogStatisticsDto
    {
        public int TotalLogs { get; set; }
        public int ErrorLogs { get; set; }
        public int WarningLogs { get; set; }
        public int InfoLogs { get; set; }
        public int DebugLogs { get; set; }
        public int LogsLastHour { get; set; }
        public int LogsLast24Hours { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for translation management.
    /// </summary>
    public class TranslationDto
    {
        public Guid Id { get; set; }
        public string Language { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }

    /// <summary>
    /// DTO for creating a new translation.
    /// </summary>
    public class CreateTranslationDto
    {
        [Required]
        [MaxLength(10)]
        [Display(Name = "common.language")]
        public string Language { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Display(Name = "superAdmin.translationKey")]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        [Display(Name = "superAdmin.translationValue")]
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a translation.
    /// </summary>
    public class UpdateTranslationDto
    {
        [Required]
        [MaxLength(1000)]
        [Display(Name = "superAdmin.translationValue")]
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for exporting/importing translations.
    /// </summary>
    public class TranslationExportDto
    {
        public string Language { get; set; } = string.Empty;
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    }
}