using AutoMapper;
using EventForge.Server.DTOs.Documents;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document types
/// </summary>
public class DocumentTypeService : IDocumentTypeService
{
    private readonly EventForgeDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the DocumentTypeService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="mapper">AutoMapper instance</param>
    public DocumentTypeService(EventForgeDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentTypeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.DocumentTypes
            .Include(dt => dt.DefaultWarehouse)
            .OrderBy(dt => dt.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<DocumentTypeDto>>(entities);
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DocumentTypes
            .Include(dt => dt.DefaultWarehouse)
            .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

        return entity == null ? null : _mapper.Map<DocumentTypeDto>(entity);
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto> CreateAsync(CreateDocumentTypeDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<DocumentType>(createDto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        _context.DocumentTypes.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with includes
        await _context.Entry(entity)
            .Reference(dt => dt.DefaultWarehouse)
            .LoadAsync(cancellationToken);

        return _mapper.Map<DocumentTypeDto>(entity);
    }

    /// <inheritdoc />
    public async Task<DocumentTypeDto?> UpdateAsync(Guid id, UpdateDocumentTypeDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DocumentTypes
            .Include(dt => dt.DefaultWarehouse)
            .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

        if (entity == null)
            return null;

        _mapper.Map(updateDto, entity);
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DocumentTypeDto>(entity);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.DocumentTypes
            .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}