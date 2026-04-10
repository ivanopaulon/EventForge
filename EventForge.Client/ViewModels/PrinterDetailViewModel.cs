using EventForge.Client.Services.Station;
using EventForge.DTOs.Station;
using EventForge.DTOs.Common;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for Printer detail dialog following the standard BaseEntityDetailViewModel pattern.
/// </summary>
public class PrinterDetailViewModel : BaseEntityDetailViewModel<PrinterDto, CreatePrinterDto, UpdatePrinterDto>
{
    private readonly IStationService _stationService;

    public PrinterDetailViewModel(IStationService stationService, ILogger<PrinterDetailViewModel> logger)
        : base(logger)
    {
        _stationService = stationService;
    }

    protected override PrinterDto CreateNewEntity() => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        Type = "Receipt",
        Status = PrinterConfigurationStatus.Active,
        Category = PrinterCategory.Receipt,
        ConnectionType = PrinterConnectionType.Tcp,
        IsThermal = true
    };

    protected override Task<PrinterDto?> LoadEntityFromServiceAsync(Guid entityId, CancellationToken ct = default)
        => _stationService.GetPrinterByIdAsync(entityId, ct);

    protected override Task LoadRelatedEntitiesAsync(Guid entityId, CancellationToken ct = default)
        => Task.CompletedTask;

    protected override CreatePrinterDto MapToCreateDto(PrinterDto entity) => new()
    {
        Name = entity.Name,
        Type = entity.Type,
        Model = entity.Model,
        Location = entity.Location,
        Address = entity.Address,
        Status = entity.Status,
        StationId = entity.StationId,
        IsFiscalPrinter = entity.IsFiscalPrinter,
        ProtocolType = entity.ProtocolType,
        Port = entity.Port,
        BaudRate = entity.BaudRate,
        SerialPortName = entity.SerialPortName,
        ConnectionType = entity.ConnectionType,
        AgentId = entity.AgentId,
        UsbDeviceId = entity.UsbDeviceId,
        Category = entity.Category,
        IsThermal = entity.IsThermal,
        PrinterWidth = entity.PrinterWidth,
        PaperWidth = entity.PaperWidth,
        PrintLanguage = entity.PrintLanguage
    };

    protected override UpdatePrinterDto MapToUpdateDto(PrinterDto entity) => new()
    {
        Name = entity.Name,
        Type = entity.Type,
        Model = entity.Model,
        Location = entity.Location,
        Address = entity.Address,
        Status = entity.Status,
        StationId = entity.StationId,
        IsFiscalPrinter = entity.IsFiscalPrinter,
        ProtocolType = entity.ProtocolType,
        Port = entity.Port,
        BaudRate = entity.BaudRate,
        SerialPortName = entity.SerialPortName,
        ConnectionType = entity.ConnectionType,
        AgentId = entity.AgentId,
        UsbDeviceId = entity.UsbDeviceId,
        Category = entity.Category,
        IsThermal = entity.IsThermal,
        PrinterWidth = entity.PrinterWidth,
        PaperWidth = entity.PaperWidth,
        PrintLanguage = entity.PrintLanguage
    };

    protected override Task<PrinterDto?> CreateEntityAsync(CreatePrinterDto createDto, CancellationToken ct = default)
        => _stationService.CreatePrinterAsync(createDto, ct);

    protected override Task<PrinterDto?> UpdateEntityAsync(Guid entityId, UpdatePrinterDto updateDto, CancellationToken ct = default)
        => _stationService.UpdatePrinterAsync(entityId, updateDto, ct);

    protected override Guid GetEntityId(PrinterDto entity) => entity.Id;
}
