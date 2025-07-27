using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.SuperAdmin;

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
/// DTO for tenant search and filtering.
/// </summary>
public class TenantSearchDto
{
    [MaxLength(100)]
    public string? SearchTerm { get; set; }

    public string? Status { get; set; } // "all", "active", "inactive"

    public int? MaxUsers { get; set; }

    public DateTime? CreatedAfter { get; set; }

    public DateTime? CreatedBefore { get; set; }

    public bool? NearUserLimit { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? SortBy { get; set; } = "CreatedAt";

    public string? SortOrder { get; set; } = "desc";
}

/// <summary>
/// DTO for tenant limits configuration.
/// </summary>
public class TenantLimitsDto
{
    public Guid TenantId { get; set; }
    public int MaxUsers { get; set; }
    public int CurrentUsers { get; set; }
    public long MaxStorageBytes { get; set; }
    public long CurrentStorageBytes { get; set; }
    public int MaxEventsPerMonth { get; set; }
    public int CurrentEventsThisMonth { get; set; }
    public bool IsNearUserLimit => CurrentUsers >= (MaxUsers * 0.9);
    public bool IsNearStorageLimit => CurrentStorageBytes >= (MaxStorageBytes * 0.9);
}

/// <summary>
/// DTO for updating tenant limits.
/// </summary>
public class UpdateTenantLimitsDto
{
    [Range(1, int.MaxValue)]
    public int MaxUsers { get; set; }

    [Range(1000000, long.MaxValue)] // Min 1MB
    public long MaxStorageBytes { get; set; }

    [Range(1, int.MaxValue)]
    public int MaxEventsPerMonth { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for tenant detailed information.
/// </summary>
public class TenantDetailDto : TenantResponseDto
{
    public TenantLimitsDto Limits { get; set; } = new();
    public List<TenantAdminResponseDto> Admins { get; set; } = new();
    public TenantUsageStatsDto UsageStats { get; set; } = new();
    public List<string> RecentActivities { get; set; } = new();
}

/// <summary>
/// DTO for tenant usage statistics.
/// </summary>
public class TenantUsageStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalEvents { get; set; }
    public int EventsThisMonth { get; set; }
    public long StorageUsedBytes { get; set; }
    public DateTime? LastActivity { get; set; }
    public int LoginAttemptsToday { get; set; }
    public int FailedLoginsToday { get; set; }
}