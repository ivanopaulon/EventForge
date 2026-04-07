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
    private bool _manualDirtyFlag = false;

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

    protected override async Task<ProductDto?> UpdateEntityAsync(Guid entityId, UpdateProductDto updateDto)
    {
        var result = await _productService.UpdateProductAsync(entityId, updateDto);
        if (result != null)
        {
            Logger.LogDebug("UpdateEntityAsync: Resetting manual dirty flag after successful save");
            _manualDirtyFlag = false;
        }
        return result;
    }

    protected override Guid GetEntityId(ProductDto entity)
    {
        return entity.Id;
    }

    /// <summary>
    /// Override to include manual dirty flag for reliable change tracking
    /// </summary>
    public override bool HasUnsavedChanges()
    {
        if (_manualDirtyFlag)
        {
            Logger.LogDebug("HasUnsavedChanges: Manual dirty flag is set");
            return true;
        }
        return base.HasUnsavedChanges();
    }

    /// <summary>
    /// Override to set manual dirty flag when entity changes
    /// </summary>
    public override void NotifyEntityChanged()
    {
        Logger.LogDebug("NotifyEntityChanged: Setting manual dirty flag");
        _manualDirtyFlag = true;
        base.NotifyEntityChanged();
    }

    /// <summary>
    /// Override LoadEntityAsync to reset manual dirty flag
    /// </summary>
    public override async Task LoadEntityAsync(Guid entityId)
    {
        _manualDirtyFlag = false;
        await base.LoadEntityAsync(entityId);
    }
}
