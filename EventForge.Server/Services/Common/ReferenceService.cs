using EventForge.Server.DTOs.Common;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Tenants;
using EventForge.Server.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service implementation for managing references
/// </summary>
public class ReferenceService : IReferenceService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ReferenceService> _logger;

    /// <summary>
    /// Initializes a new instance of the ReferenceService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="auditLogService">Audit log service</param>
    /// <param name="tenantContext">Tenant context service</param>
    /// <param name="logger">Logger instance</param>
    public ReferenceService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<ReferenceService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PagedResult<ReferenceDto>> GetReferencesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Add automated tests for tenant isolation in reference queries
            var currentTenantId = _tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for reference operations.");
            }

            var skip = (page - 1) * pageSize;

            var totalCount = await _context.References
                .WhereActiveTenant(currentTenantId.Value)
                .LongCountAsync(cancellationToken);

            var entities = await _context.References
                .WhereActiveTenant(currentTenantId.Value)
                .OrderBy(r => r.LastName)
                .ThenBy(r => r.FirstName)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = ReferenceMapper.ToDtoList(entities);

            return new PagedResult<ReferenceDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving references.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReferenceDto>> GetReferencesByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.References
                .Where(r => r.OwnerId == ownerId)
                .OrderBy(r => r.LastName)
                .ThenBy(r => r.FirstName)
                .ToListAsync(cancellationToken);

            return ReferenceMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving references for owner {OwnerId}.", ownerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ReferenceDto?> GetReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.References
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            return entity == null ? null : ReferenceMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reference with ID {ReferenceId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ReferenceDto> CreateReferenceAsync(CreateReferenceDto createReferenceDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createReferenceDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = ReferenceMapper.ToEntity(createReferenceDto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = currentUser;

            _context.References.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Reference {ReferenceId} created by {User}.", entity.Id, currentUser);

            return ReferenceMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reference for user {User}.", currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ReferenceDto?> UpdateReferenceAsync(Guid id, UpdateReferenceDto updateReferenceDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateReferenceDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalEntity = await _context.References
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (originalEntity == null)
            {
                _logger.LogWarning("Reference with ID {ReferenceId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var entity = await _context.References
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Reference with ID {ReferenceId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            ReferenceMapper.UpdateEntity(entity, updateReferenceDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            _logger.LogInformation("Reference {ReferenceId} updated by {User}.", id, currentUser);

            return ReferenceMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reference {ReferenceId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteReferenceAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalEntity = await _context.References
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (originalEntity == null)
            {
                _logger.LogWarning("Reference with ID {ReferenceId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var entity = await _context.References
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Reference with ID {ReferenceId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, originalEntity, cancellationToken);

            _logger.LogInformation("Reference {ReferenceId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reference {ReferenceId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReferenceExistsAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.References
                .AnyAsync(r => r.Id == referenceId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if reference {ReferenceId} exists.", referenceId);
            throw;
        }
    }
}