using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for DocumentType detail page
/// </summary>
public class DocumentTypeDetailViewModel : BaseEntityDetailViewModel<DocumentTypeDto, CreateDocumentTypeDto, UpdateDocumentTypeDto>
{
    private readonly IDocumentTypeService _documentTypeService;
    private readonly IWarehouseService _warehouseService;

    public DocumentTypeDetailViewModel(
        IDocumentTypeService documentTypeService,
        IWarehouseService warehouseService,
        ILogger<DocumentTypeDetailViewModel> logger)
        : base(logger)
    {
        _documentTypeService = documentTypeService;
        _warehouseService = warehouseService;
    }

    // Related entity collections
    public IEnumerable<StorageFacilityDto>? Warehouses { get; private set; }

    protected override DocumentTypeDto CreateNewEntity()
    {
        return new DocumentTypeDto
        {
            Id = Guid.Empty,
            Code = string.Empty,
            Name = string.Empty,
            IsStockIncrease = false,
            IsFiscal = true,
            RequiredPartyType = BusinessPartyType.Both,
            DefaultWarehouseId = null,
            DefaultWarehouseName = null,
            Notes = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null
        };
    }

    protected override async Task<DocumentTypeDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _documentTypeService.GetDocumentTypeByIdAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        if (IsNewEntity)
        {
            Warehouses = new List<StorageFacilityDto>();
            return;
        }

        try
        {
            // Load warehouses for default warehouse dropdown
            var warehousesResult = await _warehouseService.GetStorageFacilitiesAsync(1, 100);
            Warehouses = warehousesResult?.Items ?? new List<StorageFacilityDto>();

            Logger.LogInformation("Loaded {Count} warehouses for document type {Id}",
                Warehouses.Count(), entityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for document type {Id}", entityId);
            Warehouses = new List<StorageFacilityDto>();
        }
    }

    protected override CreateDocumentTypeDto MapToCreateDto(DocumentTypeDto entity)
    {
        return new CreateDocumentTypeDto
        {
            Code = entity.Code,
            Name = entity.Name,
            IsStockIncrease = entity.IsStockIncrease,
            DefaultWarehouseId = entity.DefaultWarehouseId,
            IsFiscal = entity.IsFiscal,
            RequiredPartyType = entity.RequiredPartyType,
            Notes = entity.Notes
        };
    }

    protected override UpdateDocumentTypeDto MapToUpdateDto(DocumentTypeDto entity)
    {
        return new UpdateDocumentTypeDto
        {
            Code = entity.Code,
            Name = entity.Name,
            IsStockIncrease = entity.IsStockIncrease,
            DefaultWarehouseId = entity.DefaultWarehouseId,
            IsFiscal = entity.IsFiscal,
            RequiredPartyType = entity.RequiredPartyType,
            Notes = entity.Notes
        };
    }

    protected override Task<DocumentTypeDto?> CreateEntityAsync(CreateDocumentTypeDto createDto)
    {
        return _documentTypeService.CreateDocumentTypeAsync(createDto);
    }

    protected override Task<DocumentTypeDto?> UpdateEntityAsync(Guid entityId, UpdateDocumentTypeDto updateDto)
    {
        return _documentTypeService.UpdateDocumentTypeAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(DocumentTypeDto entity)
    {
        return entity.Id;
    }
}
