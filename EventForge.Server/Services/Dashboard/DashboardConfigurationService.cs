using EventForge.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;
using EntityDashboard = EventForge.Server.Data.Entities.Dashboard;

namespace EventForge.Server.Services.Dashboard;

/// <summary>
/// Service for managing dashboard configurations.
/// </summary>
public class DashboardConfigurationService : IDashboardConfigurationService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DashboardConfigurationService> _logger;

    public DashboardConfigurationService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DashboardConfigurationService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<List<DashboardConfigurationDto>> GetConfigurationsAsync(string entityType, CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required");

            return await _context.DashboardConfigurations
                .AsNoTracking()
                .Include(c => c.Metrics)
                .Where(c => c.TenantId == tenantId
                    && c.UserId == userId
                    && c.EntityType == entityType
                    && !c.IsDeleted)
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .Select(c => new DashboardConfigurationDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    EntityType = c.EntityType,
                    IsDefault = c.IsDefault,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.ModifiedAt ?? c.CreatedAt,
                    Metrics = c.Metrics
                        .Where(m => !m.IsDeleted)
                        .OrderBy(m => m.Order)
                        .Select(m => new DashboardMetricConfigDto
                        {
                            Title = m.Title,
                            Type = (DTOs.Dashboard.MetricType)m.Type,
                            FieldName = m.FieldName,
                            FilterCondition = m.FilterCondition,
                            Format = m.Format,
                            Icon = m.Icon,
                            Color = m.Color,
                            Description = m.Description,
                            Order = m.Order
                        })
                        .ToList()
                })
                .ToListAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetConfigurationsAsync operation was cancelled");
            throw;
        }
    }

    public async Task<DashboardConfigurationDto?> GetConfigurationByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required");

            return await _context.DashboardConfigurations
                .AsNoTracking()
                .Include(c => c.Metrics)
                .Where(c => c.Id == id
                    && c.TenantId == tenantId
                    && c.UserId == userId
                    && !c.IsDeleted)
                .Select(c => new DashboardConfigurationDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    EntityType = c.EntityType,
                    IsDefault = c.IsDefault,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.ModifiedAt ?? c.CreatedAt,
                    Metrics = c.Metrics
                        .Where(m => !m.IsDeleted)
                        .OrderBy(m => m.Order)
                        .Select(m => new DashboardMetricConfigDto
                        {
                            Title = m.Title,
                            Type = (DTOs.Dashboard.MetricType)m.Type,
                            FieldName = m.FieldName,
                            FilterCondition = m.FilterCondition,
                            Format = m.Format,
                            Icon = m.Icon,
                            Color = m.Color,
                            Description = m.Description,
                            Order = m.Order
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetConfigurationByIdAsync operation was cancelled for ID: {Id}", id);
            throw;
        }
    }

    public async Task<DashboardConfigurationDto?> GetDefaultConfigurationAsync(string entityType, CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required");

            return await _context.DashboardConfigurations
                .AsNoTracking()
                .Include(c => c.Metrics)
                .Where(c => c.TenantId == tenantId
                    && c.UserId == userId
                    && c.EntityType == entityType
                    && c.IsDefault
                    && !c.IsDeleted)
                .Select(c => new DashboardConfigurationDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    EntityType = c.EntityType,
                    IsDefault = c.IsDefault,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.ModifiedAt ?? c.CreatedAt,
                    Metrics = c.Metrics
                        .Where(m => !m.IsDeleted)
                        .OrderBy(m => m.Order)
                        .Select(m => new DashboardMetricConfigDto
                        {
                            Title = m.Title,
                            Type = (DTOs.Dashboard.MetricType)m.Type,
                            FieldName = m.FieldName,
                            FilterCondition = m.FilterCondition,
                            Format = m.Format,
                            Icon = m.Icon,
                            Color = m.Color,
                            Description = m.Description,
                            Order = m.Order
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GetDefaultConfigurationAsync operation was cancelled for entity type: {EntityType}", entityType);
            throw;
        }
    }

    public async Task<DashboardConfigurationDto> CreateConfigurationAsync(CreateDashboardConfigurationDto dto, CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required");
            var username = GetCurrentUsername();

            // If this should be default, unset other defaults
            if (dto.IsDefault)
            {
                await UnsetDefaultsForEntityTypeAsync(dto.EntityType, ct);
            }

            var configuration = new EntityDashboard.DashboardConfiguration
            {
                TenantId = tenantId,
                UserId = userId,
                Name = dto.Name,
                Description = dto.Description,
                EntityType = dto.EntityType,
                IsDefault = dto.IsDefault,
                CreatedBy = username,
                CreatedAt = DateTime.UtcNow
            };

            // Add metrics
            var order = 0;
            foreach (var metricDto in dto.Metrics.OrderBy(m => m.Order))
            {
                configuration.Metrics.Add(new EntityDashboard.DashboardMetricConfig
                {
                    TenantId = tenantId,
                    Title = metricDto.Title,
                    Type = (EntityDashboard.MetricType)metricDto.Type,
                    FieldName = metricDto.FieldName,
                    FilterCondition = metricDto.FilterCondition,
                    Format = metricDto.Format,
                    Icon = metricDto.Icon,
                    Color = metricDto.Color,
                    Description = metricDto.Description,
                    Order = order++,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.DashboardConfigurations.Add(configuration);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Dashboard configuration created: {ConfigurationId} for user {UserId}",
                configuration.Id, userId);

            return MapToDto(configuration);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("CreateConfigurationAsync operation was cancelled");
            throw;
        }
    }

    public async Task<DashboardConfigurationDto> UpdateConfigurationAsync(Guid id, UpdateDashboardConfigurationDto dto, CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required");
            var username = GetCurrentUsername();

            var configuration = await _context.DashboardConfigurations
                .Include(c => c.Metrics)
                .FirstOrDefaultAsync(c => c.Id == id
                    && c.TenantId == tenantId
                    && c.UserId == userId
                    && !c.IsDeleted, ct);

            if (configuration == null)
            {
                throw new InvalidOperationException("Dashboard configuration not found");
            }

            // If this should be default, unset other defaults
            if (dto.IsDefault && !configuration.IsDefault)
            {
                await UnsetDefaultsForEntityTypeAsync(configuration.EntityType, ct);
            }

            configuration.Name = dto.Name;
            configuration.Description = dto.Description;
            configuration.IsDefault = dto.IsDefault;
            configuration.ModifiedBy = username;
            configuration.ModifiedAt = DateTime.UtcNow;

            // Remove old metrics
            _context.DashboardMetricConfigs.RemoveRange(configuration.Metrics);

            // Add new metrics
            configuration.Metrics.Clear();
            var order = 0;
            foreach (var metricDto in dto.Metrics.OrderBy(m => m.Order))
            {
                configuration.Metrics.Add(new EntityDashboard.DashboardMetricConfig
                {
                    TenantId = tenantId,
                    Title = metricDto.Title,
                    Type = (EntityDashboard.MetricType)metricDto.Type,
                    FieldName = metricDto.FieldName,
                    FilterCondition = metricDto.FilterCondition,
                    Format = metricDto.Format,
                    Icon = metricDto.Icon,
                    Color = metricDto.Color,
                    Description = metricDto.Description,
                    Order = order++,
                    CreatedBy = username,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Dashboard configuration updated: {ConfigurationId}", configuration.Id);

            return MapToDto(configuration);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UpdateConfigurationAsync operation was cancelled for ID: {Id}", id);
            throw;
        }
    }

    public async Task DeleteConfigurationAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required");
            var username = GetCurrentUsername();

            var configuration = await _context.DashboardConfigurations
                .FirstOrDefaultAsync(c => c.Id == id
                    && c.TenantId == tenantId
                    && c.UserId == userId
                    && !c.IsDeleted, ct);

            if (configuration == null)
            {
                throw new InvalidOperationException("Dashboard configuration not found");
            }

            configuration.IsDeleted = true;
            configuration.DeletedBy = username;
            configuration.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Dashboard configuration deleted: {ConfigurationId}", configuration.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("DeleteConfigurationAsync operation was cancelled for ID: {Id}", id);
            throw;
        }
    }

    public async Task SetAsDefaultAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required");

            var configuration = await _context.DashboardConfigurations
                .FirstOrDefaultAsync(c => c.Id == id
                    && c.TenantId == tenantId
                    && c.UserId == userId
                    && !c.IsDeleted, ct);

            if (configuration == null)
            {
                throw new InvalidOperationException("Dashboard configuration not found");
            }

            await UnsetDefaultsForEntityTypeAsync(configuration.EntityType, ct);

            configuration.IsDefault = true;
            configuration.ModifiedBy = GetCurrentUsername();
            configuration.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Dashboard configuration set as default: {ConfigurationId}", configuration.Id);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SetAsDefaultAsync operation was cancelled for ID: {Id}", id);
            throw;
        }
    }

    private async Task UnsetDefaultsForEntityTypeAsync(string entityType, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var tenantId = _tenantContext.CurrentTenantId ?? throw new InvalidOperationException("Tenant context is required");
        var username = GetCurrentUsername();

        var defaultConfigs = await _context.DashboardConfigurations
            .Where(c => c.TenantId == tenantId
                && c.UserId == userId
                && c.EntityType == entityType
                && c.IsDefault
                && !c.IsDeleted)
            .ToListAsync(ct);

        foreach (var config in defaultConfigs)
        {
            config.IsDefault = false;
            config.ModifiedBy = username;
            config.ModifiedAt = DateTime.UtcNow;
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException("User ID not found in claims");
        }
        return userId;
    }

    private string GetCurrentUsername()
    {
        var username = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username))
        {
            return "System";
        }
        return username;
    }

    private static DashboardConfigurationDto MapToDto(EntityDashboard.DashboardConfiguration entity)
    {
        return new DashboardConfigurationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            EntityType = entity.EntityType,
            IsDefault = entity.IsDefault,
            UserId = entity.UserId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.ModifiedAt ?? entity.CreatedAt,
            Metrics = entity.Metrics
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Order)
                .Select(m => new DashboardMetricConfigDto
                {
                    Title = m.Title,
                    Type = (DTOs.Dashboard.MetricType)m.Type,
                    FieldName = m.FieldName,
                    FilterCondition = m.FilterCondition,
                    Format = m.Format,
                    Icon = m.Icon,
                    Color = m.Color,
                    Description = m.Description,
                    Order = m.Order
                })
                .ToList()
        };
    }
}
