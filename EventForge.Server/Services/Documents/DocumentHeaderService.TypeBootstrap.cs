using EventForge.Server.Mappers;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Documents;


namespace EventForge.Server.Services.Documents;

public partial class DocumentHeaderService
{
    public async Task<DocumentTypeDto> GetOrCreateInventoryDocumentTypeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing inventory document type
        var existingType = await context.DocumentTypes
            .AsNoTracking()
            .Where(dt => dt.TenantId == tenantId && dt.Code == "INVENTORY" && !dt.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingType is not null)
        {
            return DocumentTypeMapper.ToDto(existingType);
        }

        // Create new inventory document type
        var newType = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "INVENTORY",
            Name = "Inventory Document",
            Notes = "Physical inventory count document",
            IsInventoryDocument = true,
            // Inventory documents record absolute quantity anchors, NOT incremental stock deltas.
            // Setting CreatesStockMovements = false prevents approve/close from generating
            // erroneous Inbound/Outbound movements for these documents.
            CreatesStockMovements = false,
            IsStockIncrease = false,
            IsActive = true,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };

        _ = context.DocumentTypes.Add(newType);
        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created inventory document type for tenant {TenantId}.", tenantId);

        return DocumentTypeMapper.ToDto(newType);
    }

    /// <summary>
    /// Gets or creates a receipt document type for sales.
    /// </summary>
    public async Task<DocumentTypeDto> GetOrCreateReceiptDocumentTypeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing receipt document type
        var existingType = await context.DocumentTypes
            .AsNoTracking()
            .Where(dt => dt.TenantId == tenantId && dt.Code == "RECEIPT" && !dt.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingType is not null)
        {
            return DocumentTypeMapper.ToDto(existingType);
        }

        // Create new receipt document type
        var newType = new DocumentType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "RECEIPT",
            Name = "Receipt Document",
            Notes = "Sales receipt document",
            IsActive = true,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };

        _ = context.DocumentTypes.Add(newType);
        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created receipt document type for tenant {TenantId}.", tenantId);

        return DocumentTypeMapper.ToDto(newType);
    }

    /// <summary>
    /// Gets or creates a system business party for internal operations.
    /// </summary>
    public async Task<Guid> GetOrCreateSystemBusinessPartyAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing system business party
        var existingParty = await context.BusinessParties
            .Where(bp => bp.TenantId == tenantId && bp.Name == "System Internal" && !bp.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingParty is not null)
        {
            return existingParty.Id;
        }

        // Create new system business party
        var newParty = new BusinessParty
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "System Internal",
            PartyType = EventForge.Server.Data.Entities.Business.BusinessPartyType.Cliente,
            IsActive = true,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };

        _ = context.BusinessParties.Add(newParty);
        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created system business party for tenant {TenantId}.", tenantId);

        return newParty.Id;
    }

}
