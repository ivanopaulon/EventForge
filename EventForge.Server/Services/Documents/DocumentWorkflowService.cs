using EventForge.DTOs.Documents;
using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document workflows
/// </summary>
public class DocumentWorkflowService : IDocumentWorkflowService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentWorkflowService> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentWorkflowService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="auditLogService">Audit log service</param>
    /// <param name="logger">Logger</param>
    public DocumentWorkflowService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<DocumentWorkflowService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentWorkflows
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
            _logger.LogError(ex, "Error retrieving document workflows.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<DocumentWorkflowDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.DocumentWorkflows
                .Include(dw => dw.DocumentType)
                .Include(dw => dw.StepDefinitions)
                .Include(dw => dw.WorkflowExecutions)
                .FirstOrDefaultAsync(dw => dw.Id == id && dw.IsActive, cancellationToken);

            return entity == null ? null : DocumentWorkflowMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document workflow {WorkflowId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetByDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentWorkflows
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
            _logger.LogError(ex, "Error retrieving document workflows for document type {DocumentTypeId}.", documentTypeId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetActiveWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentWorkflows
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
            _logger.LogError(ex, "Error retrieving active document workflows.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentWorkflowDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);

            var entities = await _context.DocumentWorkflows
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
            _logger.LogError(ex, "Error retrieving document workflows for category {Category}.", category);
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

            _context.DocumentWorkflows.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync<DocumentWorkflow>(entity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            await _context.Entry(entity)
                .Reference(dw => dw.DocumentType)
                .LoadAsync(cancellationToken);

            _logger.LogInformation("Document workflow {WorkflowId} created by {User}.", entity.Id, currentUser);

            return DocumentWorkflowMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document workflow for user {User}.", currentUser);
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

            var entity = await _context.DocumentWorkflows
                .Include(dw => dw.DocumentType)
                .Include(dw => dw.StepDefinitions)
                .Include(dw => dw.WorkflowExecutions)
                .FirstOrDefaultAsync(dw => dw.Id == id && dw.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document workflow {WorkflowId} not found for update.", id);
                return null;
            }

            var originalValues = entity.ToString();

            DocumentWorkflowMapper.UpdateEntity(entity, updateDto);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync<DocumentWorkflow>(entity, "Update", currentUser, null, cancellationToken);

            _logger.LogInformation("Document workflow {WorkflowId} updated by {User}.", id, currentUser);

            return DocumentWorkflowMapper.ToDto(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document workflow {WorkflowId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await _context.DocumentWorkflows
                .FirstOrDefaultAsync(dw => dw.Id == id && dw.IsActive, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document workflow {WorkflowId} not found for deletion.", id);
                return false;
            }

            // Soft delete
            entity.IsActive = false;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync<DocumentWorkflow>(entity, "SoftDelete", currentUser, null, cancellationToken);

            _logger.LogInformation("Document workflow {WorkflowId} soft deleted by {User}.", id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document workflow {WorkflowId} for user {User}.", id, currentUser);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetActiveStatusAsync(Guid id, bool isActive, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var entity = await _context.DocumentWorkflows
                .FirstOrDefaultAsync(dw => dw.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document workflow {WorkflowId} not found for status update.", id);
                return false;
            }

            entity.IsActive = isActive;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync<DocumentWorkflow>(entity, "StatusUpdate", currentUser, null, cancellationToken);

            _logger.LogInformation("Document workflow {WorkflowId} status updated to {Status} by {User}.", id, isActive, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for document workflow {WorkflowId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int?> GetLatestVersionAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.DocumentWorkflows
                .FirstOrDefaultAsync(dw => dw.Id == workflowId, cancellationToken);

            return entity?.WorkflowVersion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest version for document workflow {WorkflowId}.", workflowId);
            throw;
        }
    }
}