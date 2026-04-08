using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service implementation for managing addresses.
/// </summary>
public class AddressService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<AddressService> logger) : IAddressService
{

    public async Task<PagedResult<AddressDto>> GetAddressesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for address operations.");
            }

            var query = context.Addresses
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var addresses = await query
                .OrderBy(a => a.OwnerType)
                .ThenBy(a => a.City)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var addressDtos = addresses.Select(MapToAddressDto);

            return new PagedResult<AddressDto>
            {
                Items = addressDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving addresses.");
            throw;
        }
    }

    public async Task<IEnumerable<AddressDto>> GetAddressesByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for address operations.");
            }

            var addresses = await context.Addresses
                .AsNoTracking()
                .Where(a => a.OwnerId == ownerId && !a.IsDeleted && a.TenantId == currentTenantId.Value)
                .OrderBy(a => a.AddressType)
                .ToListAsync(cancellationToken);

            return addresses.Select(MapToAddressDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving addresses for owner {OwnerId}.", ownerId);
            throw;
        }
    }

    public async Task<AddressDto?> GetAddressByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var address = await context.Addresses
                .AsNoTracking()
                .Where(a => a.Id == id && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return address is null ? null : MapToAddressDto(address);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving address {AddressId}.", id);
            throw;
        }
    }

    public async Task<AddressDto> CreateAddressAsync(CreateAddressDto createAddressDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createAddressDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for address operations.");
            }

            var address = new Address
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId.Value,
                OwnerId = createAddressDto.OwnerId,
                OwnerType = createAddressDto.OwnerType,
                AddressType = createAddressDto.AddressType.ToEntity(),
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

            _ = context.Addresses.Add(address);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(address, "Create", currentUser, null, cancellationToken);

            logger.LogInformation("Address {AddressId} created by {User}.", address.Id, currentUser);

            return MapToAddressDto(address);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating address.");
            throw;
        }
    }

    public async Task<AddressDto?> UpdateAddressAsync(Guid id, UpdateAddressDto updateAddressDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateAddressDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var address = await context.Addresses
                .AsNoTracking()
                .Where(a => a.Id == id && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (address is null) return null;

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(address).CurrentValues.Clone();
            var originalAddress = (Address)originalValues.ToObject();

            address.AddressType = updateAddressDto.AddressType.ToEntity();
            address.Street = updateAddressDto.Street;
            address.City = updateAddressDto.City;
            address.ZipCode = updateAddressDto.ZipCode;
            address.Province = updateAddressDto.Province;
            address.Country = updateAddressDto.Country;
            address.Notes = updateAddressDto.Notes;
            address.ModifiedAt = DateTime.UtcNow;
            address.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Address {AddressId}.", id);
                throw new InvalidOperationException("L'indirizzo è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(address, "Update", currentUser, originalAddress, cancellationToken);

            logger.LogInformation("Address {AddressId} updated by {User}.", address.Id, currentUser);

            return MapToAddressDto(address);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating address {AddressId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteAddressAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var address = await context.Addresses
                .AsNoTracking()
                .Where(a => a.Id == id && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (address is null) return false;

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(address).CurrentValues.Clone();
            var originalAddress = (Address)originalValues.ToObject();

            address.IsDeleted = true;
            address.DeletedAt = DateTime.UtcNow;
            address.DeletedBy = currentUser;
            address.ModifiedAt = DateTime.UtcNow;
            address.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting Address {AddressId}.", id);
                throw new InvalidOperationException("L'indirizzo è stato modificato da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(address, "Delete", currentUser, originalAddress, cancellationToken);

            logger.LogInformation("Address {AddressId} deleted by {User}.", address.Id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting address {AddressId}.", id);
            throw;
        }
    }

    public async Task<bool> AddressExistsAsync(Guid addressId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Addresses
                .AsNoTracking()
                .AnyAsync(a => a.Id == addressId && !a.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if address {AddressId} exists.", addressId);
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
            AddressType = address.AddressType.ToDto(),
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
