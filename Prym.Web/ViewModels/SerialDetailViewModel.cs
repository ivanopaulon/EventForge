using Prym.DTOs.Products;
using Prym.DTOs.Warehouse;
using Prym.Web.Services;

namespace Prym.Web.ViewModels;

/// <summary>
/// ViewModel for Serial (Matricola) detail dialog.
/// </summary>
public class SerialDetailViewModel : BaseEntityDetailViewModel<SerialDto, CreateSerialDto, UpdateSerialDto>
{
    private readonly ISerialService _serialService;
    private readonly IProductService _productService;
    private readonly ILotService _lotService;
    private const int ProductSearchPageSize = 50;
    private const int MaxLotDropdownItems = 200;

    public SerialDetailViewModel(
        ISerialService serialService,
        IProductService productService,
        ILotService lotService,
        ILogger<SerialDetailViewModel> logger)
        : base(logger)
    {
        _serialService = serialService;
        _productService = productService;
        _lotService = lotService;
    }

    public IEnumerable<ProductDto> Products { get; private set; } = Array.Empty<ProductDto>();
    public ProductDto? SelectedProduct { get; private set; }
    public IEnumerable<LotDto>? Lots { get; private set; }
    public Guid? LoadedLotsProductId { get; private set; }

    /// <summary>
    /// Optional pre-set product ID (used when opening dialog from product context).
    /// </summary>
    public Guid? InitialProductId { get; set; }

    protected override SerialDto CreateNewEntity()
    {
        return new SerialDto
        {
            Id = Guid.Empty,
            TenantId = Guid.Empty,
            SerialNumber = string.Empty,
            ProductId = InitialProductId ?? Guid.Empty,
            Status = "Available",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    protected override async Task<SerialDto?> LoadEntityFromServiceAsync(Guid entityId, CancellationToken ct = default)
    {
        return await _serialService.GetSerialByIdAsync(entityId, ct);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId, CancellationToken ct = default)
    {
        await LoadDropdownDataAsync(ct);
    }

    public override async Task LoadEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        await base.LoadEntityAsync(entityId, ct);

        // For new entities we still need the products/lots lists for dropdowns
        if (IsNewEntity)
        {
            await LoadDropdownDataAsync(ct);
        }
    }

    private async Task LoadDropdownDataAsync(CancellationToken ct = default)
    {
        await EnsureProductSeedLoadedAsync(ct);
        await InitializeSelectedProductAsync(ct);
        await RefreshLotsForProductAsync(SelectedProduct?.Id, notifyStateChanged: false, ct);
    }

    private async Task EnsureProductSeedLoadedAsync(CancellationToken ct = default)
    {
        if (Products.Any())
            return;

        try
        {
            var result = await _productService.GetProductsAsync(1, ProductSearchPageSize, ct: ct);
            Products = result?.Items ?? Array.Empty<ProductDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading seed products for serial");
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
            return;

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
            Logger.LogError(ex, "Error initializing selected product for serial");
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
            return Array.Empty<ProductDto>();

        try
        {
            var result = await _productService.GetProductsAsync(1, ProductSearchPageSize, searchTerm.Trim(), ct: ct);
            return result?.Items ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching products for serial");
            return Array.Empty<ProductDto>();
        }
    }

    public async Task SetSelectedProductAsync(ProductDto? product, CancellationToken ct = default)
    {
        SelectedProduct = product;
        if (product is not null && !Products.Any(p => p.Id == product.Id))
        {
            var products = Products.ToList();
            products.Add(product);
            Products = products;
        }

        if (Entity == null)
            return;

        Entity.ProductId = product?.Id ?? Guid.Empty;
        Entity.ProductName = product?.Name;
        Entity.ProductCode = product?.Code;
        Entity.LotId = null;
        Entity.LotCode = null;
        NotifyEntityChanged();

        await RefreshLotsForProductAsync(Entity.ProductId, notifyStateChanged: false, ct);
    }

    public async Task RefreshLotsForProductAsync(Guid? productId, bool notifyStateChanged = true, CancellationToken ct = default)
    {
        var normalizedProductId = productId.HasValue && productId.Value != Guid.Empty
            ? productId
            : null;

        if (Lots != null && LoadedLotsProductId == normalizedProductId)
        {
            if (notifyStateChanged)
                NotifyStateChanged();
            return;
        }

        try
        {
            var lotsResult = await _lotService.GetLotsAsync(1, MaxLotDropdownItems, productId: normalizedProductId, ct: ct);
            Lots = lotsResult?.Items?.ToList() ?? new List<LotDto>();
            LoadedLotsProductId = normalizedProductId;

            if (Entity != null && Entity.LotId.HasValue && !Lots.Any(l => l.Id == Entity.LotId.Value))
            {
                Entity.LotId = null;
                Entity.LotCode = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading lots for serial");
            Lots = new List<LotDto>();
            LoadedLotsProductId = normalizedProductId;
        }

        if (notifyStateChanged)
        {
            NotifyStateChanged();
        }
    }

    protected override CreateSerialDto MapToCreateDto(SerialDto entity)
    {
        return new CreateSerialDto
        {
            SerialNumber = entity.SerialNumber,
            ProductId = entity.ProductId,
            LotId = entity.LotId,
            ManufacturingDate = entity.ManufacturingDate,
            WarrantyExpiry = entity.WarrantyExpiry,
            Notes = entity.Notes,
            Barcode = entity.Barcode,
            RfidTag = entity.RfidTag
        };
    }

    protected override UpdateSerialDto MapToUpdateDto(SerialDto entity)
    {
        return new UpdateSerialDto
        {
            SerialNumber = entity.SerialNumber,
            LotId = entity.LotId,
            CurrentLocationId = entity.CurrentLocationId,
            ManufacturingDate = entity.ManufacturingDate,
            WarrantyExpiry = entity.WarrantyExpiry,
            Notes = entity.Notes,
            Barcode = entity.Barcode,
            RfidTag = entity.RfidTag,
            Status = entity.Status,
            IsActive = entity.IsActive
        };
    }

    protected override Task<SerialDto?> CreateEntityAsync(CreateSerialDto createDto, CancellationToken ct = default)
    {
        return _serialService.CreateSerialAsync(createDto, ct);
    }

    protected override Task<SerialDto?> UpdateEntityAsync(Guid entityId, UpdateSerialDto updateDto, CancellationToken ct = default)
    {
        return _serialService.UpdateSerialAsync(entityId, updateDto, ct);
    }

    protected override Guid GetEntityId(SerialDto entity)
    {
        return entity.Id;
    }
}
