using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Settings;

/// <summary>
/// Enhanced configuration DTO with versioning and hot-reload support.
/// </summary>
public class ConfigurationValueDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresRestart { get; set; }
    public bool CanHotReload => !RequiresRestart;
    public bool IsEncrypted { get; set; }
    public bool IsReadOnly { get; set; }
    public ConfigurationSource Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Configuration source priority.
/// </summary>
public enum ConfigurationSource
{
    AppsettingsFile = 1,    // Original appsettings.json
    OverridesFile = 2,      // appsettings.overrides.json
    Database = 3            // SystemConfiguration table (highest priority)
}

/// <summary>
/// Configuration category enumeration.
/// </summary>
public enum ConfigurationCategory
{
    General,
    Logging,
    Performance,
    Pagination,
    Security,
    Database,
    Email,
    Features
}

/// <summary>
/// Request DTO for updating a configuration value.
/// </summary>
public class UpdateConfigurationRequest
{
    [Required]
    public string Value { get; set; } = string.Empty;
    
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for batch updating multiple configuration values.
/// </summary>
public class BatchUpdateConfigurationRequest
{
    [Required]
    public Dictionary<string, string> Changes { get; set; } = new();
    
    public string? Reason { get; set; }
}

/// <summary>
/// Result DTO for configuration change operations.
/// </summary>
public class ConfigurationChangeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Key { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public int NewVersion { get; set; }
    public bool RequiresRestart { get; set; }
    public bool HotReloadApplied { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Configuration version history entry.
/// </summary>
public class ConfigurationVersionDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// Configuration diff between two versions.
/// </summary>
public class ConfigurationDiffDto
{
    public string Key { get; set; } = string.Empty;
    public int Version1 { get; set; }
    public int Version2 { get; set; }
    public string? Value1 { get; set; }
    public string? Value2 { get; set; }
    public DateTime? Timestamp1 { get; set; }
    public DateTime? Timestamp2 { get; set; }
    public string? ModifiedBy1 { get; set; }
    public string? ModifiedBy2 { get; set; }
    public bool HasChanges => Value1 != Value2;
}

/// <summary>
/// Export format enumeration.
/// </summary>
public enum ConfigurationExportFormat
{
    Json,
    EnvironmentVariables
}

/// <summary>
/// Result DTO for configuration import operations.
/// </summary>
public class ConfigurationImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> AppliedChanges { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalChanges => AppliedChanges.Count;
    public bool IsDryRun { get; set; }
}

/// <summary>
/// JWT key information DTO.
/// </summary>
public class JwtKeyInfoDto
{
    public Guid Id { get; set; }
    public string KeyIdentifier { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow;
    public bool IsValid => IsActive && !IsExpired && ValidFrom <= DateTime.UtcNow;
}

/// <summary>
/// Request DTO for JWT key rotation.
/// </summary>
public class RotateJwtKeyRequest
{
    [Required]
    public string NewKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Overlap period in hours during which both old and new keys are valid.
    /// </summary>
    [Range(1, 168)] // 1 hour to 7 days
    public int OverlapHours { get; set; } = 24;
}

/// <summary>
/// Database connection status DTO.
/// </summary>
public class DatabaseConnectionStatusDto
{
    public bool IsConnected { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ServerVersion { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public double ConnectionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Migration information DTO.
/// </summary>
public class MigrationInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AppliedAt { get; set; }
    public bool IsApplied { get; set; }
}

/// <summary>
/// Migration result DTO.
/// </summary>
public class MigrationResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> AppliedMigrations { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bootstrap operation result DTO.
/// </summary>
public class BootstrapResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database statistics DTO.
/// </summary>
public class DatabaseStatisticsDto
{
    public long TotalTables { get; set; }
    public long TotalRecords { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public Dictionary<string, long> TableSizes { get; set; } = new();
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Server restart status DTO.
/// </summary>
public class RestartStatusDto
{
    public bool RestartRequired { get; set; }
    public List<string> PendingChanges { get; set; } = new();
    public RestartEnvironment Environment { get; set; }
    public DateTime? LastRestartAt { get; set; }
    public TimeSpan? Uptime { get; set; }
}

/// <summary>
/// Server restart result DTO.
/// </summary>
public class RestartResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public RestartMethod Method { get; set; }
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Restart method enumeration.
/// </summary>
public enum RestartMethod
{
    IisWebConfig,      // Touch web.config
    IisAppPoolRecycle, // iisreset
    KestrelStopApp,    // ApplicationLifetime.StopApplication()
    DockerRestart,     // External (container orchestration)
    Manual             // User must restart
}

/// <summary>
/// Restart environment enumeration.
/// </summary>
public enum RestartEnvironment
{
    IIS,
    Kestrel,
    Docker,
    Unknown
}

/// <summary>
/// System operation log DTO.
/// </summary>
public class SystemOperationLogDto
{
    public Guid Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
