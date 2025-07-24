using EventForge.DTOs.Banks;
using EventForge.Services.Audit;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Services.Banks;

/// <summary>
/// Service implementation for managing banks.
/// </summary>
public class BankService : IBankService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<BankService> _logger;

    public BankService(EventForgeDbContext context, IAuditLogService auditLogService, ILogger<BankService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<BankDto>> GetBanksAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Banks
                .Where(b => !b.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            var banks = await query
                .OrderBy(b => b.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var bankDtos = banks.Select(MapToBankDto);

            return new PagedResult<BankDto>
            {
                Items = bankDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving banks.");
            throw;
        }
    }

    public async Task<BankDto?> GetBankByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var bank = await _context.Banks
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            return bank != null ? MapToBankDto(bank) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bank {BankId}.", id);
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

            _context.Banks.Add(bank);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(bank, "Insert", currentUser, null, cancellationToken);

            _logger.LogInformation("Bank {BankId} created by {User}.", bank.Id, currentUser);

            return MapToBankDto(bank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bank.");
            throw;
        }
    }

    public async Task<BankDto?> UpdateBankAsync(Guid id, UpdateBankDto updateBankDto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(updateBankDto);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalBank = await _context.Banks
                .AsNoTracking()
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBank == null) return null;

            var bank = await _context.Banks
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (bank == null) return null;

            bank.Name = updateBankDto.Name;
            bank.Code = updateBankDto.Code;
            bank.SwiftBic = updateBankDto.SwiftBic;
            bank.Branch = updateBankDto.Branch;
            bank.Address = updateBankDto.Address;
            bank.Country = updateBankDto.Country;
            bank.Phone = updateBankDto.Phone;
            bank.Email = updateBankDto.Email;
            bank.Notes = updateBankDto.Notes;
            bank.ModifiedAt = DateTime.UtcNow;
            bank.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(bank, "Update", currentUser, originalBank, cancellationToken);

            _logger.LogInformation("Bank {BankId} updated by {User}.", bank.Id, currentUser);

            return MapToBankDto(bank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bank {BankId}.", id);
            throw;
        }
    }

    public async Task<bool> DeleteBankAsync(Guid id, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(currentUser);

            var originalBank = await _context.Banks
                .AsNoTracking()
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (originalBank == null) return false;

            var bank = await _context.Banks
                .Where(b => b.Id == id && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (bank == null) return false;

            bank.IsDeleted = true;
            bank.ModifiedAt = DateTime.UtcNow;
            bank.ModifiedBy = currentUser;

            await _context.SaveChangesAsync(cancellationToken);

            await _auditLogService.TrackEntityChangesAsync(bank, "Delete", currentUser, originalBank, cancellationToken);

            _logger.LogInformation("Bank {BankId} deleted by {User}.", bank.Id, currentUser);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bank {BankId}.", id);
            throw;
        }
    }

    public async Task<bool> BankExistsAsync(Guid bankId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Banks
                .AnyAsync(b => b.Id == bankId && !b.IsDeleted, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if bank {BankId} exists.", bankId);
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