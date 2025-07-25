using AutoMapper;
using EventForge.Server.DTOs.Common;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service implementation for managing references
/// </summary>
public class ReferenceService : IReferenceService
{
    private readonly EventForgeDbContext _context;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the ReferenceService
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="mapper">AutoMapper instance</param>
    public ReferenceService(EventForgeDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<PagedResult<ReferenceDto>> GetReferencesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;

        var totalCount = await _context.References
            .LongCountAsync(cancellationToken);

        var entities = await _context.References
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<ReferenceDto>>(entities);

        return new PagedResult<ReferenceDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ReferenceDto>> GetReferencesByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var entities = await _context.References
            .Where(r => r.OwnerId == ownerId)
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<ReferenceDto>>(entities);
    }

    /// <inheritdoc />
    public async Task<ReferenceDto?> GetReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.References
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return entity == null ? null : _mapper.Map<ReferenceDto>(entity);
    }

    /// <inheritdoc />
    public async Task<ReferenceDto> CreateReferenceAsync(CreateReferenceDto createReferenceDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<Reference>(createReferenceDto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedBy = currentUser;

        _context.References.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ReferenceDto>(entity);
    }

    /// <inheritdoc />
    public async Task<ReferenceDto?> UpdateReferenceAsync(Guid id, UpdateReferenceDto updateReferenceDto, string currentUser, CancellationToken cancellationToken = default)
    {
        var entity = await _context.References
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity == null)
            return null;

        _mapper.Map(updateReferenceDto, entity);
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ReferenceDto>(entity);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteReferenceAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        var entity = await _context.References
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.ModifiedAt = DateTime.UtcNow;
        entity.ModifiedBy = currentUser;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ReferenceExistsAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        return await _context.References
            .AnyAsync(r => r.Id == referenceId, cancellationToken);
    }
}