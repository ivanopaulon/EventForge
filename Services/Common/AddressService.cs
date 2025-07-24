using EventForge.Data.Entities.Common;
using EventForge.Models.Common;
using EventForge.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Services.Common;

/// <summary>
/// Service implementation for managing addresses.
/// </summary>
public class AddressService : IAddressService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AddressService> _logger;

    public AddressService(EventForgeDbContext context, IAuditLogService auditLogService, ILogger<AddressService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<AddressDto>> GetAddressesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Addresses
                .Where(a => !a.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var addresses = await query
                .OrderBy(a => a.OwnerType)
                .ThenBy(a => a.City)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var addressDtos = addresses.Select(MapToAddressDto);

            return new PagedResult<AddressDto>
            {
                Items = addressDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving addresses.");
            throw;
        }
    }

    public async Task<IEnumerable<AddressDto>> GetAddressesByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var addresses = await _context.Addresses
                .Where(a => a.OwnerId == ownerId && !a.IsDeleted)
                .OrderBy(a => a.AddressType)
                .ToListAsync(cancellationToken);

            return addresses.Select(MapToAddressDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving addresses for owner {OwnerId}.", ownerId);
            throw;
        }
    }

    public async Task<AddressDto?> GetAddressByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var address = await _context.Addresses
                .Where(a => a.Id == id && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return address == null ? null : MapToAddressDto(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address {AddressId}.", id);
            throw;
        }
    }

    public async Task<AddressDto> CreateAddressAsync(CreateAddressDto createAddressDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createAddressDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var address = new Address
            {
                Id = Guid.NewGuid(),
                OwnerId = createAddressDto.OwnerId,
                OwnerType = createAddressDto.OwnerType,
                AddressType = createAddressDto.AddressType,
                Street = createAddressDto.Street,
                City = createAddressDto.City,
                ZipCode = createAddressDto.ZipCode,
                Province = createAddressDto.Province,
                Country = createAddressDto.Country,
                Notes = createAddressDto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser,
                IsActive = true
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(address, "Create", currentUser, null, cancellationToken);

            _logger.LogInformation("Address {AddressId} created by {User}.", address.Id, currentUser);

            return MapToAddressDto(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating address.");
            throw;
        }
    }

    public async Task<AddressDto?> UpdateAddressAsync(Guid id, UpdateAddressDto updateAddressDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateAddressDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalAddress = await _context.Addresses
                .AsNoTracking()
                .Where(a => a.Id == id && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalAddress == null) return null;

            var address = await _context.Addresses
                .Where(a => a.Id == id && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (address == null) return null;

            address.AddressType = updateAddressDto.AddressType;
            address.Street = updateAddressDto.Street;
            address.City = updateAddressDto.City;
            address.ZipCode = updateAddressDto.ZipCode;
            address.Province = updateAddressDto.Province;
            address.Country = updateAddressDto.Country;
            address.Notes = updateAddressDto.Notes;
            address.ModifiedAt = DateTime.UtcNow;
            address.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(address, "Update", currentUser, originalAddress, cancellationToken);

            _logger.LogInformation("Address {AddressId} updated by {User}.", address.Id, currentUser);

            return MapToAddressDto(address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address {AddressId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteAddressAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalAddress = await _context.Addresses
                .AsNoTracking()
                .Where(a => a.Id == id && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalAddress == null) return false;

            var address = await _context.Addresses
                .Where(a => a.Id == id && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (address == null) return false;

            address.IsDeleted = true;
            address.DeletedAt = DateTime.UtcNow;
            address.DeletedBy = currentUser;
            address.ModifiedAt = DateTime.UtcNow;
            address.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(address, "Delete", currentUser, originalAddress, cancellationToken);

            _logger.LogInformation("Address {AddressId} deleted by {User}.", address.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId}.", id);
            throw;
        }
    }

    public async Task<bool> AddressExistsAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Addresses
                .AnyAsync(a => a.Id == addressId && !a.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if address {AddressId} exists.", addressId);
            throw;
        }
    }

    private static AddressDto MapToAddressDto(Address address)
    {
        return new AddressDto
        {
            Id = address.Id,
            OwnerId = address.OwnerId,
            OwnerType = address.OwnerType,
            AddressType = address.AddressType,
            Street = address.Street,
            City = address.City,
            ZipCode = address.ZipCode,
            Province = address.Province,
            Country = address.Country,
            Notes = address.Notes,
            CreatedAt = address.CreatedAt,
            CreatedBy = address.CreatedBy,
            ModifiedAt = address.ModifiedAt,
            ModifiedBy = address.ModifiedBy
        };
    }
}