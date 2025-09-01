using EventForge.DTOs.Documents;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document templates
/// </summary>
public class DocumentTemplateService : IDocumentTemplateService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentTemplateService> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentTemplateService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="auditLogService">Audit log service</param>
    /// <param name="logger">Logger</param>
    public DocumentTemplateService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<DocumentTemplateService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentTemplates
                .Include(dt => dt.DocumentType)
                .Where(dt => dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document templates.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.DocumentTemplates
                .Include(dt => dt.DocumentType)
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.IsActive, cancellationToken);

            return entity == null ? null : DocumentTemplateMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document template {TemplateId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetByDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentTemplates
                .Include(dt => dt.DocumentType)
                .Where(dt => dt.DocumentTypeId == documentTypeId && dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document templates for document type {DocumentTypeId}.", documentTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentTemplates
                .Include(dt => dt.DocumentType)
                .Where(dt => dt.IsPublic && dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public document templates.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(owner);

            var entities = await _context.DocumentTemplates
                .Include(dt => dt.DocumentType)
                .Where(dt => (dt.Owner == owner || dt.IsPublic) && dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document templates for owner {Owner}.", owner);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);

            var entities = await _context.DocumentTemplates
                .Include(dt => dt.DocumentType)
                .Where(dt => dt.Category == category && dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document templates for category {Category}.", category);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto> CreateAsync(CreateDocumentTemplateDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = DocumentTemplateMapper.ToEntity(createDto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = currentUser;

            _context.DocumentTemplates.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync<DocumentTemplate>(entity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            await _context.Entry(entity)
                .Reference(dt => dt.DocumentType)
                .LoadAsync(cancellationToken);

            _logger.LogInformation("Document template {TemplateId} created by {User}.", entity.Id, currentUser);

            return DocumentTemplateMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document template for user {User}.", currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto?> UpdateAsync(Guid id, UpdateDocumentTemplateDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await _context.DocumentTemplates
                .Include(dt => dt.DocumentType)
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document template {TemplateId} not found for update.", id);
                return null;
            }

            var originalValues = entity.ToString();

            DocumentTemplateMapper.UpdateEntity(entity, updateDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync<DocumentTemplate>(entity, "Update", currentUser, null, cancellationToken);

            _logger.LogInformation("Document template {TemplateId} updated by {User}.", id, currentUser);

            return DocumentTemplateMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document template {TemplateId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await _context.DocumentTemplates
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document template {TemplateId} not found for deletion.", id);
                return false;
            }

            // Soft delete
            entity.IsActive = false;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync<DocumentTemplate>(entity, "SoftDelete", currentUser, null, cancellationToken);

            _logger.LogInformation("Document template {TemplateId} soft deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document template {TemplateId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUsageAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await _context.DocumentTemplates
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document template {TemplateId} not found for usage update.", id);
                return false;
            }

            entity.UsageCount++;
            entity.LastUsedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document template {TemplateId} usage updated by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating usage for document template {TemplateId}.", id);
            throw;
        }
    }
}