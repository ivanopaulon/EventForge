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

    public IEnumerable<ProductDto>? Products { get; private set; }
    public IEnumerable<LotDto>? Lots { get; private set; }

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
        try
        {
            var productsResult = await _productService.GetProductsAsync(1, 200, ct: ct);
            Products = productsResult?.Items ?? new List<ProductDto>();

            var lotsResult = await _lotService.GetLotsAsync(1, 500, ct: ct);
            Lots = lotsResult?.Items ?? new List<LotDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading dropdown data for serial");
            Products = new List<ProductDto>();
            Lots = new List<LotDto>();
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
            ManufacturingDate = entity.ManufacturingDate,
            WarrantyExpiry = entity.WarrantyExpiry,
            Notes = entity.Notes,
            Barcode = entity.Barcode,
            RfidTag = entity.RfidTag,
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
