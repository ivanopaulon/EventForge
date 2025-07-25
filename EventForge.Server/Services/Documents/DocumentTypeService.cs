using AutoMapper;
using EventForge.Server.DTOs.Documents;
using EventForge.Server.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document types
/// </summary>
public class DocumentTypeService : IDocumentTypeService
{
    private readonly EventForgeDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DocumentTypeService> _logger;

    /// <summary>
    /// Initializes a new instance of the DocumentTypeService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="mapper">AutoMapper instance</param>
    /// <param name="auditLogService">Audit log service</param>
    /// <param name="logger">Logger</param>
    public DocumentTypeService(
        EventForgeDbContext context,
        IMapper mapper,
        IAuditLogService auditLogService,
        ILogger<DocumentTypeService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.DocumentTypes
                .Include(dt => dt.DefaultWarehouse)
                .OrderBy(dt => dt.Name)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IEnumerable<DocumentTypeDto>>(entities);
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
            var entity = await _context.DocumentTypes
                .Include(dt => dt.DefaultWarehouse)
                .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

            return entity == null ? null : _mapper.Map<DocumentTypeDto>(entity);
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

            var entity = _mapper.Map<DocumentType>(createDto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            entity.CreatedBy = currentUser;

            _context.DocumentTypes.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entity, "Insert", currentUser, null, cancellationToken);

            // Reload with includes
            await _context.Entry(entity)
                .Reference(dt => dt.DefaultWarehouse)
                .LoadAsync(cancellationToken);

            _logger.LogInformation("Document type {DocumentTypeId} created by {User}.", entity.Id, currentUser);

            return _mapper.Map<DocumentTypeDto>(entity);
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

            var originalEntity = await _context.DocumentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

            if (originalEntity == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            var entity = await _context.DocumentTypes
                .Include(dt => dt.DefaultWarehouse)
                .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found for update by user {User}.", id, currentUser);
                return null;
            }

            _mapper.Map(updateDto, entity);
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entity, "Update", currentUser, originalEntity, cancellationToken);

            _logger.LogInformation("Document type {DocumentTypeId} updated by {User}.", id, currentUser);

            return _mapper.Map<DocumentTypeDto>(entity);
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

            var originalEntity = await _context.DocumentTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

            if (originalEntity == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            var entity = await _context.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

            if (entity == null)
            {
                _logger.LogWarning("Document type with ID {DocumentTypeId} not found for deletion by user {User}.", id, currentUser);
                return false;
            }

            entity.IsDeleted = true;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(entity, "Delete", currentUser, originalEntity, cancellationToken);

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