using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service implementation for managing references
/// </summary>
public class ReferenceService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<ReferenceService> logger) : IReferenceService
{
    /// <inheritdoc />
    public async Task<PagedResult<ReferenceDto>> GetReferencesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for reference operations.");
            }

            var skip = (page - 1) * pageSize;

            var totalCount = await context.References
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .LongCountAsync(cancellationToken);

            var entities = await context.References
                .AsNoTracking()
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
            logger.LogError(ex, "Error retrieving references.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReferenceDto>> GetReferencesByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for reference operations.");
            }

            var entities = await context.References
                .AsNoTracking()
                .Where(r => r.OwnerId == ownerId && !r.IsDeleted && r.TenantId == currentTenantId.Value)
                .OrderBy(r => r.LastName)
                .ThenBy(r => r.FirstName)
                .ToListAsync(cancellationToken);

            return ReferenceMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving references for owner {OwnerId}.", ownerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ReferenceDto?> GetReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await context.References
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            return entity is null ? null : ReferenceMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving reference with ID {ReferenceId}.", id);
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

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for reference operations.");
            }

            var entity = ReferenceMapper.ToEntity(createReferenceDto);
            entity.Id = Guid.NewGuid();
            entity.TenantId = currentTenantId.Value;
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = currentUser;

            _ = context.References.Add(entity);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Reference {ReferenceId} created by {User}.", entity.Id, currentUser);

            return ReferenceMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating reference for user {User}.", currentUser);
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

            var entity = await context.References
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Reference with ID {ReferenceId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(entity).CurrentValues.Clone();
            var originalEntity = (Reference)originalValues.ToObject();

            ReferenceMapper.UpdateEntity(entity, updateReferenceDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Reference {ReferenceId}.", id);
                throw new InvalidOperationException("Il riferimento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            logger.LogInformation("Reference {ReferenceId} updated by {User}.", id, currentUser);

            return ReferenceMapper.ToDto(entity);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating reference {ReferenceId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteReferenceAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await context.References
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Reference with ID {ReferenceId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(entity).CurrentValues.Clone();
            var originalEntity = (Reference)originalValues.ToObject();

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting Reference {ReferenceId}.", id);
                throw new InvalidOperationException("Il riferimento è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, originalEntity, cancellationToken);

            logger.LogInformation("Reference {ReferenceId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting reference {ReferenceId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReferenceExistsAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.References
                .AsNoTracking()
                .AnyAsync(r => r.Id == referenceId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if reference {ReferenceId} exists.", referenceId);
            throw;
        }
    }

}
