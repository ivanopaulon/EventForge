using EventForge.DTOs.Documents;
using EventForge.Server.Mappers;
using EventForge.Server.Services.Caching;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document types
/// </summary>
public class DocumentTypeService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<DocumentTypeService> logger,
    ICacheService cacheService) : IDocumentTypeService
{

    private const string CACHE_KEY_ALL = "DocumentTypes_All";

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning("Cannot retrieve document types without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            // Cache all DocumentTypes for 30 minutes
            var allDocumentTypes = await cacheService.GetOrCreateAsync(
                CACHE_KEY_ALL,
                tenantId.Value,
                async (ct) =>
                {
                    var entities = await context.DocumentTypes
                        .AsNoTracking()
                        .Include(dt => dt.DefaultWarehouse)
                        .Where(dt => dt.TenantId == tenantId.Value)
                        .OrderBy(dt => dt.Name)
                        .ToListAsync(ct);

                    return DocumentTypeMapper.ToDtoCollection(entities).ToList();
                },
                absoluteExpiration: TimeSpan.FromMinutes(30),
                ct: cancellationToken
            );

            return allDocumentTypes;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning("Cannot retrieve document type without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var entity = await context.DocumentTypes
                .AsNoTracking()
                .Include(dt => dt.DefaultWarehouse)
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            return entity is null ? null : DocumentTypeMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto> CreateAsync(CreateDocumentTypeDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning("Cannot create document type without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var entity = DocumentTypeMapper.ToEntity(createDto);
            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId.Value;
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = currentUser;

            _ = context.DocumentTypes.Add(entity);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            await context.Entry(entity)
                .Reference(dt => dt.DefaultWarehouse)
                .LoadAsync(cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, tenantId.Value);

            logger.LogInformation("Document type {DocumentTypeId} created by {User}.", entity.Id, currentUser);

            return DocumentTypeMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> UpdateAsync(Guid id, UpdateDocumentTypeDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning("Cannot update document type without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var originalEntity = await context.DocumentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            if (originalEntity is null)
            {
                logger.LogWarning("Document type with ID {DocumentTypeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var entity = await context.DocumentTypes
                .Include(dt => dt.DefaultWarehouse)
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Document type with ID {DocumentTypeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            DocumentTypeMapper.UpdateEntity(entity, updateDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, tenantId.Value);

            logger.LogInformation("Document type {DocumentTypeId} updated by {User}.", id, currentUser);

            return DocumentTypeMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var tenantId = tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                logger.LogWarning("Cannot delete document type without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var originalEntity = await context.DocumentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            if (originalEntity is null)
            {
                logger.LogWarning("Document type with ID {DocumentTypeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var entity = await context.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Document type with ID {DocumentTypeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, originalEntity, cancellationToken);

            // Invalidate cache
            cacheService.Invalidate(CACHE_KEY_ALL, tenantId.Value);

            logger.LogInformation("Document type {DocumentTypeId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
