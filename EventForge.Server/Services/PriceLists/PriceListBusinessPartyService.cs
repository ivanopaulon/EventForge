using EventForge.DTOs.PriceLists;
using Microsoft.EntityFrameworkCore;
using PriceListBusinessParty = EventForge.Server.Data.Entities.PriceList.PriceListBusinessParty;
using PriceListBusinessPartyStatus = EventForge.Server.Data.Entities.PriceList.PriceListBusinessPartyStatus;

namespace EventForge.Server.Services.PriceLists;

public class PriceListBusinessPartyService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ILogger<PriceListBusinessPartyService> logger) : IPriceListBusinessPartyService
{

    #region Phase 2A/2B - BusinessParty Assignment Methods

    public async Task<PriceListBusinessPartyDto> AssignBusinessPartyAsync(Guid priceListId, AssignBusinessPartyToPriceListDto dto, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Assigning BusinessParty {BusinessPartyId} to PriceList {PriceListId}", dto.BusinessPartyId, priceListId);

            // Validate PriceList exists
            var priceList = await context.PriceLists.AsNoTracking()
                .FirstOrDefaultAsync(pl => pl.Id == priceListId && !pl.IsDeleted, cancellationToken);

            if (priceList is null)
            {
                logger.LogWarning("PriceList {PriceListId} not found", priceListId);
                throw new InvalidOperationException($"PriceList with ID {priceListId} not found");
            }

            // Validate BusinessParty exists
            var businessParty = await context.BusinessParties.AsNoTracking()
                .FirstOrDefaultAsync(bp => bp.Id == dto.BusinessPartyId && !bp.IsDeleted, cancellationToken);

            if (businessParty is null)
            {
                logger.LogWarning("BusinessParty {BusinessPartyId} not found", dto.BusinessPartyId);
                throw new InvalidOperationException($"BusinessParty with ID {dto.BusinessPartyId} not found");
            }

            // Check if already assigned
            var existingAssignment = await context.PriceListBusinessParties.AsNoTracking()
                .FirstOrDefaultAsync(plbp => plbp.PriceListId == priceListId && plbp.BusinessPartyId == dto.BusinessPartyId && !plbp.IsDeleted, cancellationToken);

            if (existingAssignment is not null)
            {
                logger.LogWarning("BusinessParty {BusinessPartyId} is already assigned to PriceList {PriceListId}", dto.BusinessPartyId, priceListId);
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

            context.PriceListBusinessParties.Add(assignment);
            await context.SaveChangesAsync(cancellationToken);

            // Log audit trail
            await auditLogService.LogEntityChangeAsync(
                "PriceListBusinessParty",
                assignment.Id,
                "Assignment",
                "Insert",
                null,
                $"Assigned BusinessParty {businessParty.Name} to PriceList {priceList.Name}",
                currentUser,
                $"{businessParty.Name} -> {priceList.Name}",
                cancellationToken);

            logger.LogInformation("BusinessParty {BusinessPartyId} successfully assigned to PriceList {PriceListId}", dto.BusinessPartyId, priceListId);

            // Return mapped DTO
            return MapToDto(assignment, businessParty);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> RemoveBusinessPartyAsync(Guid priceListId, Guid businessPartyId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Removing BusinessParty {BusinessPartyId} from PriceList {PriceListId}", businessPartyId, priceListId);

            // Find existing assignment
            var assignment = await context.PriceListBusinessParties
                .Include(plbp => plbp.BusinessParty)
                .Include(plbp => plbp.PriceList)
                .FirstOrDefaultAsync(plbp => plbp.PriceListId == priceListId && plbp.BusinessPartyId == businessPartyId && !plbp.IsDeleted, cancellationToken);

            if (assignment is null)
            {
                logger.LogWarning("PriceListBusinessParty assignment not found for PriceList {PriceListId} and BusinessParty {BusinessPartyId}", priceListId, businessPartyId);
                return false;
            }

            // Soft delete
            assignment.IsDeleted = true;
            assignment.ModifiedAt = DateTime.UtcNow;
            assignment.ModifiedBy = currentUser;
            assignment.Status = PriceListBusinessPartyStatus.Deleted;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict removing BusinessParty {BusinessPartyId} from PriceList {PriceListId}.", businessPartyId, priceListId);
                throw new InvalidOperationException("L'associazione è stata modificata da un altro utente. Ricarica la pagina e riprova.", ex);
            }

            // Log audit trail
            await auditLogService.LogEntityChangeAsync(
                "PriceListBusinessParty",
                assignment.Id,
                "Assignment",
                "Delete",
                $"Active - {assignment.BusinessParty?.Name} -> {assignment.PriceList?.Name}",
                "Deleted",
                currentUser,
                $"{assignment.BusinessParty?.Name} -> {assignment.PriceList?.Name}",
                cancellationToken);

            logger.LogInformation("BusinessParty {BusinessPartyId} successfully removed from PriceList {PriceListId}", businessPartyId, priceListId);

            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<PriceListBusinessPartyDto>> GetBusinessPartiesForPriceListAsync(Guid priceListId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Getting BusinessParties for PriceList {PriceListId}", priceListId);

            var assignments = await context.PriceListBusinessParties.AsNoTracking()
                .Include(plbp => plbp.BusinessParty)
                .Where(plbp => plbp.PriceListId == priceListId && !plbp.IsDeleted)
                .ToListAsync(cancellationToken);

            logger.LogInformation("Found {Count} BusinessParties for PriceList {PriceListId}", assignments.Count, priceListId);

            return assignments.Select(a => MapToDto(a, a.BusinessParty)).ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(Guid businessPartyId, PriceListType? type, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Getting PriceLists for BusinessParty {BusinessPartyId} with type filter: {Type}", businessPartyId, type?.ToString() ?? "None");

            var query = context.PriceListBusinessParties.AsNoTracking()
                .Include(plbp => plbp.PriceList)
                .ThenInclude(pl => pl.Event)
                .Where(plbp => plbp.BusinessPartyId == businessPartyId && !plbp.IsDeleted);

            // Apply type filter if provided
            if (type.HasValue)
            {
                query = query.Where(plbp => plbp.PriceList.Type == type.Value);
            }

            var assignments = await query.ToListAsync(cancellationToken);

            logger.LogInformation("Found {Count} PriceLists for BusinessParty {BusinessPartyId}", assignments.Count, businessPartyId);

            return assignments
                .Where(a => a.PriceList != null && !a.PriceList.IsDeleted)
                .Select(a => MapToPriceListDto(a.PriceList))
                .ToList();
        }
        catch (Exception ex)
        {
            throw;
        }
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
