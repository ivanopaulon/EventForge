using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for Product detail page
/// </summary>
public class ProductDetailViewModel : BaseEntityDetailViewModel<ProductDto, CreateProductDto, UpdateProductDto>
{
    private readonly IProductService _productService;

    public ProductDetailViewModel(
        IProductService productService,
        ILogger<ProductDetailViewModel> logger)
        : base(logger)
    {
        _productService = productService;
    }

    // Related entity collections
    public IEnumerable<ProductCodeDto>? ProductCodes { get; private set; }
    public IEnumerable<ProductUnitDto>? ProductUnits { get; private set; }
    public IEnumerable<ProductSupplierDto>? ProductSuppliers { get; private set; }
    public IEnumerable<ProductBundleItemDto>? BundleItems { get; private set; }

    protected override ProductDto CreateNewEntity()
    {
        return new ProductDto
        {
            Id = Guid.Empty,
            Code = string.Empty,
            Name = string.Empty,
            ShortDescription = string.Empty,
            Description = string.Empty,
            Status = ProductStatus.Active,
            IsVatIncluded = false,
            IsBundle = false,
            DefaultPrice = null
        };
    }

    protected override async Task<ProductDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        // Prefer detailed payload if available
        return await _productService.GetProductDetailAsync(entityId)
            ?? await _productService.GetProductByIdAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        if (IsNewEntity)
        {
            ProductCodes = new List<ProductCodeDto>();
            ProductUnits = new List<ProductUnitDto>();
            ProductSuppliers = new List<ProductSupplierDto>();
            BundleItems = new List<ProductBundleItemDto>();
            return;
        }

        try
        {
            var codesTask = _productService.GetProductCodesAsync(entityId);
            var unitsTask = _productService.GetProductUnitsAsync(entityId);
            var suppliersTask = _productService.GetProductSuppliersAsync(entityId);

            await Task.WhenAll(codesTask, unitsTask, suppliersTask);

            ProductCodes = codesTask.Result;
            ProductUnits = unitsTask.Result;
            ProductSuppliers = suppliersTask.Result;

            if (Entity?.IsBundle == true)
            {
                BundleItems = await _productService.GetProductBundleItemsAsync(entityId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for product {ProductId}", entityId);
            ProductCodes = new List<ProductCodeDto>();
            ProductUnits = new List<ProductUnitDto>();
            ProductSuppliers = new List<ProductSupplierDto>();
            BundleItems = new List<ProductBundleItemDto>();
        }
    }

    protected override CreateProductDto MapToCreateDto(ProductDto entity)
    {
        return new CreateProductDto
        {
            Code = entity.Code,
            Name = entity.Name,
            ShortDescription = entity.ShortDescription,
            Description = entity.Description,
            Status = entity.Status,
            IsVatIncluded = entity.IsVatIncluded,
            DefaultPrice = entity.DefaultPrice,
            VatRateId = entity.VatRateId,
            UnitOfMeasureId = entity.UnitOfMeasureId,
            CategoryNodeId = entity.CategoryNodeId,
            FamilyNodeId = entity.FamilyNodeId,
            GroupNodeId = entity.GroupNodeId,
            StationId = entity.StationId,
            BrandId = entity.BrandId,
            ModelId = entity.ModelId,
            PreferredSupplierId = entity.PreferredSupplierId,
            ReorderPoint = entity.ReorderPoint,
            SafetyStock = entity.SafetyStock,
            TargetStockLevel = entity.TargetStockLevel,
            AverageDailyDemand = entity.AverageDailyDemand
        };
    }

    protected override UpdateProductDto MapToUpdateDto(ProductDto entity)
    {
        return new UpdateProductDto
        {
            Name = entity.Name,
            ShortDescription = entity.ShortDescription,
            Description = entity.Description,
            Status = entity.Status,
            IsVatIncluded = entity.IsVatIncluded,
            DefaultPrice = entity.DefaultPrice,
            VatRateId = entity.VatRateId,
            UnitOfMeasureId = entity.UnitOfMeasureId,
            CategoryNodeId = entity.CategoryNodeId,
            FamilyNodeId = entity.FamilyNodeId,
            GroupNodeId = entity.GroupNodeId,
            StationId = entity.StationId,
            BrandId = entity.BrandId,
            ModelId = entity.ModelId,
            PreferredSupplierId = entity.PreferredSupplierId,
            ReorderPoint = entity.ReorderPoint,
            SafetyStock = entity.SafetyStock,
            TargetStockLevel = entity.TargetStockLevel,
            AverageDailyDemand = entity.AverageDailyDemand,
            ImageDocumentId = entity.ImageDocumentId
        };
    }

    protected override Task<ProductDto?> CreateEntityAsync(CreateProductDto createDto)
    {
        return _productService.CreateProductAsync(createDto);
    }

    protected override Task<ProductDto?> UpdateEntityAsync(Guid entityId, UpdateProductDto updateDto)
    {
        return _productService.UpdateProductAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(ProductDto entity)
    {
        return entity.Id;
    }
}
