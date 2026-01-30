using EventForge.DTOs.Documents;
using EventForge.Server.Mappers;
using EventForge.Server.Services.Caching;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document types
/// </summary>
public class DocumentTypeService : IDocumentTypeService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DocumentTypeService> _logger;
    private readonly ICacheService _cacheService;

    private const string CACHE_KEY_ALL = "DocumentTypes_All";

    /// <summary>
    /// Initializes a new instance of the DocumentTypeService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="auditLogService">Audit log service</param>
    /// <param name="tenantContext">Tenant context</param>
    /// <param name="logger">Logger</param>
    /// <param name="cacheService">Cache service</param>
    public DocumentTypeService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ITenantContext tenantContext,
        ILogger<DocumentTypeService> logger,
        ICacheService cacheService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot retrieve document types without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            // Cache all DocumentTypes for 30 minutes
            var allDocumentTypes = await _cacheService.GetOrCreateAsync(
                CACHE_KEY_ALL,
                tenantId.Value,
                async (ct) =>
                {
                    var entities = await _context.DocumentTypes
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
            _logger.LogError(ex, "Error retrieving document types.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot retrieve document type without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var entity = await _context.DocumentTypes
                .Include(dt => dt.DefaultWarehouse)
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            return entity == null ? null : DocumentTypeMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document type {DocumentTypeId}.", id);
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

            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot create document type without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var entity = DocumentTypeMapper.ToEntity(createDto);
            entity.Id = Guid.NewGuid();
            entity.TenantId = tenantId.Value;
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = currentUser;

            _ = _context.DocumentTypes.Add(entity);
            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            await _context.Entry(entity)
                .Reference(dt => dt.DefaultWarehouse)
                .LoadAsync(cancellationToken);

            // Invalidate cache
            _cacheService.Invalidate(CACHE_KEY_ALL, tenantId.Value);

            _logger.LogInformation("Document type {DocumentTypeId} created by {User}.", entity.Id, currentUser);

            return DocumentTypeMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document type for user {User}.", currentUser);
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

            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot update document type without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var originalEntity = await _context.DocumentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            if (originalEntity == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var entity = await _context.DocumentTypes
                .Include(dt => dt.DefaultWarehouse)
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            DocumentTypeMapper.UpdateEntity(entity, updateDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            // Invalidate cache
            _cacheService.Invalidate(CACHE_KEY_ALL, tenantId.Value);

            _logger.LogInformation("Document type {DocumentTypeId} updated by {User}.", id, currentUser);

            return DocumentTypeMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document type {DocumentTypeId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var tenantId = _tenantContext.CurrentTenantId;
            if (!tenantId.HasValue)
            {
                _logger.LogWarning("Cannot delete document type without a tenant context.");
                throw new InvalidOperationException("Tenant context is required.");
            }

            var originalEntity = await _context.DocumentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            if (originalEntity == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var entity = await _context.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.TenantId == tenantId.Value, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await _context.SaveChangesAsync(cancellationToken);

            _ = await _auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, originalEntity, cancellationToken);

            // Invalidate cache
            _cacheService.Invalidate(CACHE_KEY_ALL, tenantId.Value);

            _logger.LogInformation("Document type {DocumentTypeId} deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document type {DocumentTypeId} for user {User}.", id, currentUser);
            throw;
        }
    }
}