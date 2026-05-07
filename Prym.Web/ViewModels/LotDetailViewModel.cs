using Prym.DTOs.Products;
using Prym.DTOs.Warehouse;
using Prym.Web.Services;

namespace Prym.Web.ViewModels;

/// <summary>
/// ViewModel for Lot detail page
/// </summary>
public class LotDetailViewModel : BaseEntityDetailViewModel<LotDto, CreateLotDto, UpdateLotDto>
{
    private readonly ILotService _lotService;
    private readonly IProductService _productService;
    private const int ProductSearchPageSize = 50;

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
    public IEnumerable<ProductDto> Products { get; private set; } = Array.Empty<ProductDto>();
    public ProductDto? SelectedProduct { get; private set; }

    /// <summary>
    /// Optional pre-set product ID (used when opening dialog from product context).
    /// </summary>
    public Guid? InitialProductId { get; set; }

    protected override LotDto CreateNewEntity()
    {
        return new LotDto
        {
            Id = Guid.Empty,
            TenantId = Guid.Empty,
            Code = string.Empty,
            ProductId = InitialProductId ?? Guid.Empty,
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

    protected override async Task<LotDto?> LoadEntityFromServiceAsync(Guid entityId, CancellationToken ct = default)
    {
        return await _lotService.GetLotByIdAsync(entityId, ct);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId, CancellationToken ct = default)
    {
        await EnsureProductSeedLoadedAsync(ct);
        await InitializeSelectedProductAsync(ct);
    }

    public override async Task LoadEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        await base.LoadEntityAsync(entityId, ct);
    }

    private async Task EnsureProductSeedLoadedAsync(CancellationToken ct = default)
    {
        if (Products.Any())
            return;

        try
        {
            var productsResult = await _productService.GetProductsAsync(1, ProductSearchPageSize, ct: ct);
            Products = productsResult?.Items ?? Array.Empty<ProductDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading seed products for lot");
            Products = Array.Empty<ProductDto>();
        }
    }

    private async Task InitializeSelectedProductAsync(CancellationToken ct = default)
    {
        if (Entity == null || Entity.ProductId == Guid.Empty)
        {
            SelectedProduct = null;
            return;
        }

        if (SelectedProduct?.Id == Entity.ProductId)
        {
            return;
        }

        try
        {
            var product = await _productService.GetProductByIdAsync(Entity.ProductId, ct);
            SelectedProduct = product ?? new ProductDto
            {
                Id = Entity.ProductId,
                Name = Entity.ProductName ?? string.Empty,
                Code = Entity.ProductCode ?? string.Empty
            };
            if (!Products.Any(p => p.Id == SelectedProduct.Id))
            {
                var products = Products.ToList();
                products.Add(SelectedProduct);
                Products = products;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing selected product for lot");
            SelectedProduct = new ProductDto
            {
                Id = Entity.ProductId,
                Name = Entity.ProductName ?? string.Empty,
                Code = Entity.ProductCode ?? string.Empty
            };
            if (!Products.Any(p => p.Id == SelectedProduct.Id))
            {
                var products = Products.ToList();
                products.Add(SelectedProduct);
                Products = products;
            }
        }
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<ProductDto>();
        }

        try
        {
            var productsResult = await _productService.GetProductsAsync(1, ProductSearchPageSize, searchTerm.Trim(), ct: ct);
            return productsResult?.Items ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching products for lot");
            return Array.Empty<ProductDto>();
        }
    }

    public void SetSelectedProduct(ProductDto? product)
    {
        SelectedProduct = product;

        if (Entity == null)
            return;

        Entity.ProductId = product?.Id ?? Guid.Empty;
        Entity.ProductName = product?.Name;
        Entity.ProductCode = product?.Code;
        NotifyEntityChanged();
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

    protected override Task<LotDto?> CreateEntityAsync(CreateLotDto createDto, CancellationToken ct = default)
    {
        return _lotService.CreateLotAsync(createDto, ct);
    }

    protected override Task<LotDto?> UpdateEntityAsync(Guid entityId, UpdateLotDto updateDto, CancellationToken ct = default)
    {
        return _lotService.UpdateLotAsync(entityId, updateDto, ct);
    }

    protected override Guid GetEntityId(LotDto entity)
    {
        return entity.Id;
    }
}
