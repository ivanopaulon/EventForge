using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using EventForge.DTOs.Warehouse;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for Lot detail page
/// </summary>
public class LotDetailViewModel : BaseEntityDetailViewModel<LotDto, CreateLotDto, UpdateLotDto>
{
    private readonly ILotService _lotService;
    private readonly IProductService _productService;

    public LotDetailViewModel(
        ILotService lotService,
        IProductService productService,
        ILogger<LotDetailViewModel> logger) 
        : base(logger)
    {
        _lotService = lotService;
        _productService = productService;
    }

    // Related entity collections
    public IEnumerable<ProductDto>? Products { get; private set; }

    protected override LotDto CreateNewEntity()
    {
        return new LotDto
        {
            Id = Guid.Empty,
            TenantId = Guid.Empty,
            Code = string.Empty,
            ProductId = Guid.Empty,
            ProductName = null,
            ProductCode = null,
            ProductionDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            SupplierId = null,
            SupplierName = null,
            OriginalQuantity = 0,
            AvailableQuantity = 0,
            Status = "Active",
            QualityStatus = "Approved",
            Notes = null,
            Barcode = null,
            CountryOfOrigin = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null,
            IsActive = true
        };
    }

    protected override async Task<LotDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _lotService.GetLotByIdAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        if (IsNewEntity)
        {
            Products = new List<ProductDto>();
            return;
        }

        try
        {
            // Load products for dropdown selection
            var productsResult = await _productService.GetProductsAsync(1, 100);
            Products = productsResult?.Items ?? new List<ProductDto>();
            
            Logger.LogInformation("Loaded {Count} products for lot {Id}", 
                Products.Count(), entityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for lot {Id}", entityId);
            Products = new List<ProductDto>();
        }
    }

    protected override CreateLotDto MapToCreateDto(LotDto entity)
    {
        return new CreateLotDto
        {
            Code = entity.Code,
            ProductId = entity.ProductId,
            ProductionDate = entity.ProductionDate,
            ExpiryDate = entity.ExpiryDate,
            SupplierId = entity.SupplierId,
            OriginalQuantity = entity.OriginalQuantity,
            Notes = entity.Notes,
            Barcode = entity.Barcode,
            CountryOfOrigin = entity.CountryOfOrigin
        };
    }

    protected override UpdateLotDto MapToUpdateDto(LotDto entity)
    {
        return new UpdateLotDto
        {
            Code = entity.Code,
            ProductionDate = entity.ProductionDate,
            ExpiryDate = entity.ExpiryDate,
            SupplierId = entity.SupplierId,
            AvailableQuantity = entity.AvailableQuantity,
            Notes = entity.Notes,
            Barcode = entity.Barcode,
            CountryOfOrigin = entity.CountryOfOrigin,
            IsActive = entity.IsActive
        };
    }

    protected override Task<LotDto?> CreateEntityAsync(CreateLotDto createDto)
    {
        return _lotService.CreateLotAsync(createDto);
    }

    protected override Task<LotDto?> UpdateEntityAsync(Guid entityId, UpdateLotDto updateDto)
    {
        return _lotService.UpdateLotAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(LotDto entity)
    {
        return entity.Id;
    }
}
