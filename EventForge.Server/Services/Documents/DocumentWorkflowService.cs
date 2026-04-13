using Prym.DTOs.Documents;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document workflows
/// </summary>
public class DocumentWorkflowService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ILogger<DocumentWorkflowService> logger) : IDocumentWorkflowService
{

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await context.DocumentWorkflows
                .AsNoTracking()
                .Include(dw => dw.DocumentType)
                .Include(dw => dw.StepDefinitions)
                .Include(dw => dw.WorkflowExecutions)
                .Where(dw => dw.IsActive)
                .OrderBy(dw => dw.Name)
                .ToListAsync(cancellationToken);

            return DocumentWorkflowMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await context.DocumentWorkflows
                .AsNoTracking()
                .Include(dw => dw.DocumentType)
                .Include(dw => dw.StepDefinitions)
                .Include(dw => dw.WorkflowExecutions)
                .FirstOrDefaultAsync(dw => dw.Id == id && dw.IsActive, cancellationToken);

            return entity is null ? null : DocumentWorkflowMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetByDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await context.DocumentWorkflows
                .AsNoTracking()
                .Include(dw => dw.DocumentType)
                .Include(dw => dw.StepDefinitions)
                .Include(dw => dw.WorkflowExecutions)
                .Where(dw => dw.DocumentTypeId == documentTypeId && dw.IsActive)
                .OrderBy(dw => dw.Name)
                .ToListAsync(cancellationToken);

            return DocumentWorkflowMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetActiveWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await context.DocumentWorkflows
                .AsNoTracking()
                .Include(dw => dw.DocumentType)
                .Include(dw => dw.StepDefinitions)
                .Include(dw => dw.WorkflowExecutions)
                .Where(dw => dw.IsActive)
                .OrderBy(dw => dw.Name)
                .ToListAsync(cancellationToken);

            return DocumentWorkflowMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);

            var entities = await context.DocumentWorkflows
                .AsNoTracking()
                .Include(dw => dw.DocumentType)
                .Include(dw => dw.StepDefinitions)
                .Include(dw => dw.WorkflowExecutions)
                .Where(dw => dw.Category == category && dw.IsActive)
                .OrderBy(dw => dw.Name)
                .ToListAsync(cancellationToken);

            return DocumentWorkflowMapper.ToDtoCollection(entities);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto> CreateAsync(CreateDocumentWorkflowDto createDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = DocumentWorkflowMapper.ToEntity(createDto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = currentUser;

            _ = context.DocumentWorkflows.Add(entity);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync<DocumentWorkflow>(entity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            await context.Entry(entity)
                .Reference(dw => dw.DocumentType)
                .LoadAsync(cancellationToken);

            logger.LogInformation("Document workflow {WorkflowId} created by {User}.", entity.Id, currentUser);

            return DocumentWorkflowMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto?> UpdateAsync(Guid id, UpdateDocumentWorkflowDto updateDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await context.DocumentWorkflows
                .Include(dw => dw.DocumentType)
                .Include(dw => dw.StepDefinitions)
                .Include(dw => dw.WorkflowExecutions)
                .FirstOrDefaultAsync(dw => dw.Id == id && dw.IsActive, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Document workflow {WorkflowId} not found for update.", id);
                return null;
            }

            var originalValues = entity.ToString();

            DocumentWorkflowMapper.UpdateEntity(entity, updateDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync<DocumentWorkflow>(entity, "Update", currentUser, null, cancellationToken);

            logger.LogInformation("Document workflow {WorkflowId} updated by {User}.", id, currentUser);

            return DocumentWorkflowMapper.ToDto(entity);
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

            var entity = await context.DocumentWorkflows
                .FirstOrDefaultAsync(dw => dw.Id == id && dw.IsActive, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Document workflow {WorkflowId} not found for deletion.", id);
                return false;
            }

            // Soft delete
            entity.IsActive = false;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync<DocumentWorkflow>(entity, "SoftDelete", currentUser, null, cancellationToken);

            logger.LogInformation("Document workflow {WorkflowId} soft deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await context.DocumentWorkflows
                .FirstOrDefaultAsync(dw => dw.Id == id, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Document workflow {WorkflowId} not found for status update.", id);
                return false;
            }

            entity.IsActive = isActive;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync<DocumentWorkflow>(entity, "StatusUpdate", currentUser, null, cancellationToken);

            logger.LogInformation("Document workflow {WorkflowId} status updated to {Status} by {User}.", id, isActive, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int?> GetLatestVersionAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await context.DocumentWorkflows
                .AsNoTracking()
                .FirstOrDefaultAsync(dw => dw.Id == workflowId, cancellationToken);

            return entity?.WorkflowVersion;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
