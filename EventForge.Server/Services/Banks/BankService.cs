using EventForge.DTOs.Banks;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Banks;

/// <summary>
/// Service implementation for managing banks.
/// </summary>
public class BankService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<BankService> logger) : IBankService
{

    public async Task<PagedResult<BankDto>> GetBanksAsync(PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        try
        {
            // NOTE: Tenant isolation test coverage should be expanded in future test iterations
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context is required for bank operations.");
            }

            var query = context.Banks
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .Include(b => b.Addresses.Where(a => !a.IsDeleted && a.TenantId == currentTenantId.Value))
                .Include(b => b.Contacts.Where(c => !c.IsDeleted && c.TenantId == currentTenantId.Value));

            var totalCount = await query.CountAsync(cancellationToken);
            var banks = await query
                .OrderBy(b => b.Name)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .ToListAsync(cancellationToken);

            var bankDtos = banks.Select(MapToBankDto);

            return new PagedResult<BankDto>
            {
                Items = bankDtos,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving banks.");
            throw;
        }
    }

    public async Task<BankDto?> GetBankByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var bank = await context.Banks
                .AsNoTracking()
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return bank is not null ? MapToBankDto(bank) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving bank {BankId}.", id);
            throw;
        }
    }

    public async Task<BankDto> CreateBankAsync(CreateBankDto createBankDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(createBankDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var bank = new Bank
            {
                Id = Guid.NewGuid(),
                Name = createBankDto.Name,
                Code = createBankDto.Code,
                SwiftBic = createBankDto.SwiftBic,
                Branch = createBankDto.Branch,
                Address = createBankDto.Address,
                Country = createBankDto.Country,
                Phone = createBankDto.Phone,
                Email = createBankDto.Email,
                Notes = createBankDto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            _ = context.Banks.Add(bank);
            _ = await context.SaveChangesAsync(cancellationToken);

            _ = await auditLogService.TrackEntityChangesAsync(bank, "Insert", currentUser, null, cancellationToken);

            logger.LogInformation("Bank {BankId} created by {User}.", bank.Id, currentUser);

            return MapToBankDto(bank);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating bank.");
            throw;
        }
    }

    public async Task<BankDto?> UpdateBankAsync(Guid id, UpdateBankDto updateBankDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateBankDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var bank = await context.Banks
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (bank is null) return null;

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(bank).CurrentValues.Clone();
            var originalBank = (Bank)originalValues.ToObject();

            bank.Name = updateBankDto.Name;
            // Note: Code and SwiftBic are intentionally not updatable - they are regulatory identifiers
            bank.Branch = updateBankDto.Branch;
            bank.Address = updateBankDto.Address;
            bank.Country = updateBankDto.Country;
            bank.Phone = updateBankDto.Phone;
            bank.Email = updateBankDto.Email;
            bank.Notes = updateBankDto.Notes;
            bank.ModifiedAt = DateTime.UtcNow;
            bank.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict updating Bank {BankId}.", id);
                throw new InvalidOperationException("La banca è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(bank, "Update", currentUser, originalBank, cancellationToken);

            logger.LogInformation("Bank {BankId} updated by {User}.", bank.Id, currentUser);

            return MapToBankDto(bank);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating bank {BankId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteBankAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var bank = await context.Banks
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (bank is null) return false;

            // Create snapshot of original state before modifications
            var originalValues = context.Entry(bank).CurrentValues.Clone();
            var originalBank = (Bank)originalValues.ToObject();

            bank.IsDeleted = true;
            bank.ModifiedAt = DateTime.UtcNow;
            bank.ModifiedBy = currentUser;

            try
            {
                _ = await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict deleting Bank {BankId}.", id);
                throw new InvalidOperationException("La banca è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            _ = await auditLogService.TrackEntityChangesAsync(bank, "Delete", currentUser, originalBank, cancellationToken);

            logger.LogInformation("Bank {BankId} deleted by {User}.", bank.Id, currentUser);

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting bank {BankId}.", id);
            throw;
        }
    }

    public async Task<bool> BankExistsAsync(Guid bankId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Banks
                .AnyAsync(b => b.Id == bankId && !b.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking if bank {BankId} exists.", bankId);
            throw;
        }
    }

    private static BankDto MapToBankDto(Bank bank)
    {
        return new BankDto
        {
            Id = bank.Id,
            Name = bank.Name,
            Code = bank.Code,
            SwiftBic = bank.SwiftBic,
            Branch = bank.Branch,
            Address = bank.Address,
            Country = bank.Country,
            Phone = bank.Phone,
            Email = bank.Email,
            Notes = bank.Notes,
            CreatedAt = bank.CreatedAt,
            CreatedBy = bank.CreatedBy,
            ModifiedAt = bank.ModifiedAt,
            ModifiedBy = bank.ModifiedBy
        };
    }

}
