using Prym.DTOs.Documents;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document templates
/// </summary>
public class DocumentTemplateService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ILogger<DocumentTemplateService> logger) : IDocumentTemplateService
{

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await context.DocumentTemplates
                .AsNoTracking()
                .Include(dt => dt.DocumentType)
                .Where(dt => dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await context.DocumentTemplates
                .AsNoTracking()
                .Include(dt => dt.DocumentType)
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.IsActive, cancellationToken);

            return entity is null ? null : DocumentTemplateMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetByDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await context.DocumentTemplates
                .AsNoTracking()
                .Include(dt => dt.DocumentType)
                .Where(dt => dt.DocumentTypeId == documentTypeId && dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await context.DocumentTemplates
                .AsNoTracking()
                .Include(dt => dt.DocumentType)
                .Where(dt => dt.IsPublic && dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(owner);

            var entities = await context.DocumentTemplates
                .AsNoTracking()
                .Include(dt => dt.DocumentType)
                .Where(dt => (dt.Owner == owner || dt.IsPublic) && dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTemplateDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);

            var entities = await context.DocumentTemplates
                .AsNoTracking()
                .Include(dt => dt.DocumentType)
                .Where(dt => dt.Category == category && dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return DocumentTemplateMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
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

            _ = context.DocumentTemplates.Add(entity);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync<DocumentTemplate>(entity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            await context.Entry(entity)
                .Reference(dt => dt.DocumentType)
                .LoadAsync(cancellationToken);

            logger.LogInformation("Document template {TemplateId} created by {User}.", entity.Id, currentUser);

            return DocumentTemplateMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
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

            var entity = await context.DocumentTemplates
                .Include(dt => dt.DocumentType)
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.IsActive, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Document template {TemplateId} not found for update.", id);
                return null;
            }

            var originalValues = entity.ToString();

            DocumentTemplateMapper.UpdateEntity(entity, updateDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync<DocumentTemplate>(entity, "Update", currentUser, null, cancellationToken);

            logger.LogInformation("Document template {TemplateId} updated by {User}.", id, currentUser);

            return DocumentTemplateMapper.ToDto(entity);
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

            var entity = await context.DocumentTemplates
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.IsActive, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Document template {TemplateId} not found for deletion.", id);
                return false;
            }

            // Soft delete
            entity.IsActive = false;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync<DocumentTemplate>(entity, "SoftDelete", currentUser, null, cancellationToken);

            logger.LogInformation("Document template {TemplateId} soft deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUsageAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await context.DocumentTemplates
                .FirstOrDefaultAsync(dt => dt.Id == id && dt.IsActive, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Document template {TemplateId} not found for usage update.", id);
                return false;
            }

            entity.UsageCount++;
            entity.LastUsedAt = DateTime.UtcNow;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Document template {TemplateId} usage updated by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
