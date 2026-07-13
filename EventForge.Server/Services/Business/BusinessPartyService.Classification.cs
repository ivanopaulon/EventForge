using EventForge.Server.Mappers;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Business;


namespace EventForge.Server.Services.Business;

public partial class BusinessPartyService
{

    public async Task<IEnumerable<ClassificationNodeDto>> GetBusinessPartyClassificationsAsync(Guid businessPartyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context required.");
            }

            var nodes = await context.BusinessPartyClassifications
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .Where(c => c.BusinessPartyId == businessPartyId)
                .Include(c => c.ClassificationNode)
                .Where(c => !c.ClassificationNode.IsDeleted)
                .Select(c => c.ClassificationNode)
                .OrderBy(n => n.Name)
                .ThenBy(n => n.Code)
                .ToListAsync(cancellationToken);

            return nodes.Select(MapToClassificationNodeDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting classifications for business party {Id}", businessPartyId);
            throw;
        }
    }

    public async Task<ClassificationNodeDto?> AssignClassificationAsync(Guid businessPartyId, Guid classificationNodeId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context required.");
            }

            var existing = await context.BusinessPartyClassifications
                .WhereActiveTenant(currentTenantId.Value)
                .FirstOrDefaultAsync(c => c.BusinessPartyId == businessPartyId && c.ClassificationNodeId == classificationNodeId, cancellationToken);

            if (existing is not null)
            {
                var existingNode = await context.ClassificationNodes
                    .AsNoTracking()
                    .WhereActiveTenant(currentTenantId.Value)
                    .FirstOrDefaultAsync(n => n.Id == classificationNodeId, cancellationToken);

                return existingNode is null ? null : MapToClassificationNodeDto(existingNode);
            }

            var node = await context.ClassificationNodes
                .AsNoTracking()
                .WhereActiveTenant(currentTenantId.Value)
                .FirstOrDefaultAsync(n =>
                    n.Id == classificationNodeId &&
                    (n.ApplicableTo == ClassificationApplicableTo.BusinessParties ||
                     n.ApplicableTo == ClassificationApplicableTo.Both ||
                     n.ApplicableTo == ClassificationApplicableTo.All), cancellationToken);

            if (node is null)
            {
                return null;
            }

            var classification = new BusinessPartyClassification
            {
                BusinessPartyId = businessPartyId,
                ClassificationNodeId = classificationNodeId,
                TenantId = currentTenantId.Value,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUser
            };

            context.BusinessPartyClassifications.Add(classification);
            await context.SaveChangesAsync(cancellationToken);

            return MapToClassificationNodeDto(node);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning classification {NodeId} to business party {PartyId}", classificationNodeId, businessPartyId);
            throw;
        }
    }

    public async Task<bool> RemoveClassificationAsync(Guid businessPartyId, Guid classificationNodeId, string currentUser, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTenantId = tenantContext.CurrentTenantId;
            if (!currentTenantId.HasValue)
            {
                throw new InvalidOperationException("Tenant context required.");
            }

            var classification = await context.BusinessPartyClassifications
                .WhereActiveTenant(currentTenantId.Value)
                .FirstOrDefaultAsync(c => c.BusinessPartyId == businessPartyId && c.ClassificationNodeId == classificationNodeId, cancellationToken);

            if (classification is null)
            {
                return false;
            }

            classification.IsDeleted = true;
            classification.DeletedAt = DateTime.UtcNow;
            classification.DeletedBy = currentUser;
            classification.ModifiedAt = DateTime.UtcNow;
            classification.ModifiedBy = currentUser;

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing classification {NodeId} from business party {PartyId}", classificationNodeId, businessPartyId);
            throw;
        }
    }

}
