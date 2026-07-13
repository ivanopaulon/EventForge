using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using AuthAuditOperationType = Prym.DTOs.Common.AuditOperationType;


namespace EventForge.Server.Services.Tenants;

public partial class TenantService
{
    public async Task<TenantDetailDto?> GetTenantDetailsAsync(Guid tenantId)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant details.");
        }
        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant is null)
        {
            return null;
        }

        var admins = await GetTenantAdminsAsync(tenantId);
        var limits = await GetTenantLimitsAsync(tenantId);
        var usageStats = await GetTenantUsageStatsAsync(tenantId);
        var recentActivities = await GetRecentActivitiesAsync(tenantId);

        return new TenantDetailDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            DisplayName = tenant.DisplayName,
            Description = tenant.Description,
            Domain = tenant.Domain,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            IsActive = tenant.IsActive,
            SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
            CreatedAt = tenant.CreatedAt,
            CreatedBy = tenant.CreatedBy,
            ModifiedAt = tenant.ModifiedAt,
            ModifiedBy = tenant.ModifiedBy,
            Admins = admins.Select(a => new TenantAdminResponseDto
            {
                UserId = a.UserId,
                Username = a.Username,
                Email = a.Email,
                FullName = a.FullName ?? string.Empty,
                MustChangePassword = false, // Default value since not available in AdminTenantResponseDto
                GeneratedPassword = null // Only for creation
            }).ToList(),
            Limits = limits ?? new TenantLimitsDto { TenantId = tenantId },
            UsageStats = usageStats,
            RecentActivities = recentActivities
        };
    }

    public async Task<TenantLimitsDto?> GetTenantLimitsAsync(Guid tenantId)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can view tenant limits.");
        }
        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant is null)
        {
            return null;
        }

        var currentUsers = await context.Users.CountAsync(u => u.TenantId == tenantId);
        var currentStorage = await CalculateStorageUsageAsync(tenantId);
        var currentEventsThisMonth = await CalculateEventsThisMonthAsync(tenantId);

        return new TenantLimitsDto
        {
            TenantId = tenantId,
            MaxUsers = tenant.MaxUsers,
            CurrentUsers = currentUsers,
            MaxStorageBytes = 1073741824, // Default 1GB - should be configurable
            CurrentStorageBytes = currentStorage,
            MaxEventsPerMonth = 1000, // Default - should be configurable
            CurrentEventsThisMonth = currentEventsThisMonth
        };
    }

    public async Task<TenantLimitsDto> UpdateTenantLimitsAsync(Guid tenantId, UpdateTenantLimitsDto updateDto)
    {
        if (!tenantContext.IsSuperAdmin)
        {
            throw new UnauthorizedAccessException("Only super administrators can update tenant limits.");
        }
        var tenant = await context.Tenants.FindAsync(tenantId);
        if (tenant is null)
        {
            throw new ArgumentException($"Tenant {tenantId} not found.");
        }

        tenant.MaxUsers = updateDto.MaxUsers;
        tenant.ModifiedAt = DateTime.UtcNow;
        tenant.ModifiedBy = tenantContext.CurrentUserId?.ToString() ?? "System";

        _ = await context.SaveChangesAsync();

        // Create audit trail entry
        var auditTrail = new AuditTrail
        {
            PerformedByUserId = tenantContext.CurrentUserId ?? Guid.Empty,
            OperationType = AuthAuditOperationType.TenantStatusChanged,
            TargetTenantId = tenantId,
            Details = $"Tenant limits updated: MaxUsers={updateDto.MaxUsers}, MaxStorage={updateDto.MaxStorageBytes}, MaxEvents={updateDto.MaxEventsPerMonth}. Reason: {updateDto.Reason}",
            WasSuccessful = true,
            PerformedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenant.ModifiedBy
        };

        _ = context.AuditTrails.Add(auditTrail);
        _ = await context.SaveChangesAsync();

        return await GetTenantLimitsAsync(tenantId) ?? throw new InvalidOperationException("Failed to retrieve updated limits.");
    }

    private async Task<TenantUsageStatsDto> GetTenantUsageStatsAsync(Guid tenantId)
    {
        var totalUsers = await context.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId);
        var activeUsers = await context.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.IsActive);
        var totalEvents = await context.Events.AsNoTracking().CountAsync(e => e.TenantId == tenantId);
        var eventsThisMonth = await CalculateEventsThisMonthAsync(tenantId);
        var storageUsed = await CalculateStorageUsageAsync(tenantId);
        var lastActivity = await context.AuditTrails
            .AsNoTracking()
            .Where(a => a.TargetTenantId == tenantId || a.SourceTenantId == tenantId)
            .OrderByDescending(a => a.PerformedAt)
            .Select(a => a.PerformedAt)
            .FirstOrDefaultAsync();

        var today = DateTime.UtcNow.Date;
        var loginAttemptsToday = await context.AuditTrails
            .AsNoTracking()
            .CountAsync(a => a.OperationType == AuthAuditOperationType.TenantSwitch &&
                           a.PerformedAt >= today &&
                           a.SourceTenantId == tenantId);

        var failedLoginsToday = await context.AuditTrails
            .AsNoTracking()
            .CountAsync(a => a.OperationType == AuthAuditOperationType.TenantStatusChanged &&
                           a.PerformedAt >= today &&
                           a.SourceTenantId == tenantId);

        return new TenantUsageStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalEvents = totalEvents,
            EventsThisMonth = eventsThisMonth,
            StorageUsedBytes = storageUsed,
            LastActivity = lastActivity,
            LoginAttemptsToday = loginAttemptsToday,
            FailedLoginsToday = failedLoginsToday
        };
    }

    private async Task<List<string>> GetRecentActivitiesAsync(Guid tenantId)
    {
        var recentAudits = await context.AuditTrails
            .AsNoTracking()
            .Where(a => a.TargetTenantId == tenantId || a.SourceTenantId == tenantId)
            .OrderByDescending(a => a.PerformedAt)
            .Take(10)
            .Select(a => $"{a.PerformedAt:yyyy-MM-dd HH:mm} - {a.OperationType}: {a.Details}")
            .ToListAsync();

        return recentAudits;
    }

    private async Task<long> CalculateStorageUsageAsync(Guid tenantId)
    {
        // Sum actual file sizes stored in the three file-bearing entities for this tenant.
        var attachmentBytes = await context.DocumentAttachments
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .SumAsync(a => (long)a.FileSizeBytes);

        var chatAttachmentBytes = await context.MessageAttachments
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .SumAsync(a => a.FileSize);

        var documentReferenceBytes = await context.DocumentReferences
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && !d.IsDeleted)
            .SumAsync(d => (long)d.FileSizeBytes);

        return attachmentBytes + chatAttachmentBytes + documentReferenceBytes;
    }

    private async Task<int> CalculateEventsThisMonthAsync(Guid tenantId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await context.Events
            .AsNoTracking()
            .CountAsync(e => e.TenantId == tenantId && e.CreatedAt >= startOfMonth);
    }

}
