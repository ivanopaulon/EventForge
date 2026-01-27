using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.PriceList;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.UnitOfMeasures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceListEntryStatus = EventForge.Server.Data.Entities.PriceList.PriceListEntryStatus;
using PriceListStatus = EventForge.Server.Data.Entities.PriceList.PriceListStatus;
using PriceListBusinessPartyStatus = EventForge.Server.Data.Entities.PriceList.PriceListBusinessPartyStatus;
using ProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;
using PriceListBusinessParty = EventForge.Server.Data.Entities.PriceList.PriceListBusinessParty;

namespace EventForge.Server.Services.PriceLists;

public class PriceListBusinessPartyService : IPriceListBusinessPartyService
{
    private readonly EventForgeDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PriceListBusinessPartyService> _logger;

    public PriceListBusinessPartyService(
        EventForgeDbContext context,
        IAuditLogService auditLogService,
        ILogger<PriceListBusinessPartyService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Phase 2A/2B - BusinessParty Assignment Methods

    public async Task<PriceListBusinessPartyDto> AssignBusinessPartyAsync(Guid priceListId, AssignBusinessPartyToPriceListDto dto, string currentUser, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Assigning BusinessParty {BusinessPartyId} to PriceList {PriceListId}", dto.BusinessPartyId, priceListId);

        // Validate PriceList exists
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);

        if (priceList == null)
        {
            _logger.LogWarning("PriceList {PriceListId} not found", priceListId);
            throw new InvalidOperationException($"PriceList with ID {priceListId} not found");
        }

        // Validate BusinessParty exists
        var businessParty = await _context.BusinessParties
            .FirstOrDefaultAsync(bp => bp.Id == dto.BusinessPartyId && !bp.IsDeleted, cancellationToken);

        if (businessParty == null)
        {
            _logger.LogWarning("BusinessParty {BusinessPartyId} not found", dto.BusinessPartyId);
            throw new InvalidOperationException($"BusinessParty with ID {dto.BusinessPartyId} not found");
        }

        // Check if already assigned
        var existingAssignment = await _context.PriceListBusinessParties
            .FirstOrDefaultAsync(plbp => plbp.PriceListId == priceListId && plbp.BusinessPartyId == dto.BusinessPartyId && !plbp.IsDeleted, cancellationToken);

        if (existingAssignment != null)
        {
            _logger.LogWarning("BusinessParty {BusinessPartyId} is already assigned to PriceList {PriceListId}", dto.BusinessPartyId, priceListId);
            throw new InvalidOperationException($"BusinessParty is already assigned to this PriceList");
        }

        // Create new assignment
        var assignment = new PriceListBusinessParty
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            BusinessPartyId = dto.BusinessPartyId,
            IsPrimary = dto.IsPrimary,
            OverridePriority = dto.OverridePriority,
            SpecificValidFrom = dto.SpecificValidFrom,
            SpecificValidTo = dto.SpecificValidTo,
            GlobalDiscountPercentage = dto.GlobalDiscountPercentage,
            Notes = dto.Notes,
            Status = PriceListBusinessPartyStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            IsDeleted = false
        };

        _context.PriceListBusinessParties.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        // Log audit trail
        await _auditLogService.LogEntityChangeAsync(
            "PriceListBusinessParty",
            assignment.Id,
            "Assignment",
            "Insert",
            null,
            $"Assigned BusinessParty {businessParty.Name} to PriceList {priceList.Name}",
            currentUser,
            $"{businessParty.Name} -> {priceList.Name}",
            cancellationToken);

        _logger.LogInformation("BusinessParty {BusinessPartyId} successfully assigned to PriceList {PriceListId}", dto.BusinessPartyId, priceListId);

        // Return mapped DTO
        return MapToDto(assignment, businessParty);
    }

    public async Task<bool> RemoveBusinessPartyAsync(Guid priceListId, Guid businessPartyId, string currentUser, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing BusinessParty {BusinessPartyId} from PriceList {PriceListId}", businessPartyId, priceListId);

        // Find existing assignment
        var assignment = await _context.PriceListBusinessParties
            .Include(plbp => plbp.BusinessParty)
            .Include(plbp => plbp.PriceList)
            .FirstOrDefaultAsync(plbp => plbp.PriceListId == priceListId && plbp.BusinessPartyId == businessPartyId && !plbp.IsDeleted, cancellationToken);

        if (assignment == null)
        {
            _logger.LogWarning("PriceListBusinessParty assignment not found for PriceList {PriceListId} and BusinessParty {BusinessPartyId}", priceListId, businessPartyId);
            return false;
        }

        // Soft delete
        assignment.IsDeleted = true;
        assignment.ModifiedAt = DateTime.UtcNow;
        assignment.ModifiedBy = currentUser;
        assignment.Status = PriceListBusinessPartyStatus.Deleted;

        await _context.SaveChangesAsync(cancellationToken);

        // Log audit trail
        await _auditLogService.LogEntityChangeAsync(
            "PriceListBusinessParty",
            assignment.Id,
            "Assignment",
            "Delete",
            $"Active - {assignment.BusinessParty?.Name} -> {assignment.PriceList?.Name}",
            "Deleted",
            currentUser,
            $"{assignment.BusinessParty?.Name} -> {assignment.PriceList?.Name}",
            cancellationToken);

        _logger.LogInformation("BusinessParty {BusinessPartyId} successfully removed from PriceList {PriceListId}", businessPartyId, priceListId);

        return true;
    }

    public async Task<IEnumerable<PriceListBusinessPartyDto>> GetBusinessPartiesForPriceListAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting BusinessParties for PriceList {PriceListId}", priceListId);

        var assignments = await _context.PriceListBusinessParties
            .Include(plbp => plbp.BusinessParty)
            .Where(plbp => plbp.PriceListId == priceListId && !plbp.IsDeleted)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} BusinessParties for PriceList {PriceListId}", assignments.Count, priceListId);

        return assignments.Select(a => MapToDto(a, a.BusinessParty)).ToList();
    }

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(Guid businessPartyId, PriceListType? type, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting PriceLists for BusinessParty {BusinessPartyId} with type filter: {Type}", businessPartyId, type?.ToString() ?? "None");

        var query = _context.PriceListBusinessParties
            .Include(plbp => plbp.PriceList)
            .ThenInclude(pl => pl.Event)
            .Where(plbp => plbp.BusinessPartyId == businessPartyId && !plbp.IsDeleted);

        // Apply type filter if provided
        if (type.HasValue)
        {
            query = query.Where(plbp => plbp.PriceList.Type == type.Value);
        }

        var assignments = await query.ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} PriceLists for BusinessParty {BusinessPartyId}", assignments.Count, businessPartyId);

        return assignments
            .Where(a => a.PriceList != null && !a.PriceList.IsDeleted)
            .Select(a => MapToPriceListDto(a.PriceList))
            .ToList();
    }

    #endregion

    #region Private Mapping Methods

    private static PriceListBusinessPartyDto MapToDto(PriceListBusinessParty entity, BusinessParty? businessParty)
    {
        return new PriceListBusinessPartyDto
        {
            BusinessPartyId = entity.BusinessPartyId,
            BusinessPartyName = businessParty?.Name ?? string.Empty,
            BusinessPartyType = businessParty?.PartyType.ToString() ?? string.Empty,
            IsPrimary = entity.IsPrimary,
            OverridePriority = entity.OverridePriority,
            SpecificValidFrom = entity.SpecificValidFrom,
            SpecificValidTo = entity.SpecificValidTo,
            GlobalDiscountPercentage = entity.GlobalDiscountPercentage,
            Notes = entity.Notes,
            Status = entity.Status.ToString()
        };
    }

    private static PriceListDto MapToPriceListDto(Data.Entities.PriceList.PriceList priceList)
    {
        return new PriceListDto
        {
            Id = priceList.Id,
            Name = priceList.Name,
            Code = priceList.Code,
            Description = priceList.Description,
            Type = priceList.Type,
            Direction = priceList.Direction,
            ValidFrom = priceList.ValidFrom,
            ValidTo = priceList.ValidTo,
            Notes = priceList.Notes,
            Status = (EventForge.DTOs.Common.PriceListStatus)priceList.Status,
            IsDefault = priceList.IsDefault,
            Priority = priceList.Priority,
            EventId = priceList.EventId,
            EventName = priceList.Event?.Name,
            EntryCount = 0, // Not loading entries for performance
            CreatedAt = priceList.CreatedAt,
            CreatedBy = priceList.CreatedBy,
            ModifiedAt = priceList.ModifiedAt,
            ModifiedBy = priceList.ModifiedBy
        };
    }

    #endregion
}
