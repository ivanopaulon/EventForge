using Prym.Web.Services.Store;
using Prym.DTOs.Common;
using Prym.DTOs.Store;

namespace Prym.Web.ViewModels;

/// <summary>
/// ViewModel for POS Terminal detail page following the ProductDetail pattern
/// </summary>
public class PosDetailViewModel : BaseEntityDetailViewModel<StorePosDto, CreateStorePosDto, UpdateStorePosDto>
{
    private readonly IStorePosService _posService;

    public PosDetailViewModel(
        IStorePosService posService,
        ILogger<PosDetailViewModel> logger)
        : base(logger)
    {
        _posService = posService;
    }

    protected override StorePosDto CreateNewEntity()
    {
        return new StorePosDto
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Status = CashRegisterStatus.Active
        };
    }

    protected override Task<StorePosDto?> LoadEntityFromServiceAsync(Guid entityId, CancellationToken ct = default)
    {
        return _posService.GetByIdAsync(entityId, ct);
    }

    protected override CreateStorePosDto MapToCreateDto(StorePosDto entity)
    {
        return new CreateStorePosDto
        {
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            Location = entity.Location,
            Notes = entity.Notes,
            TerminalIdentifier = entity.TerminalIdentifier,
            IPAddress = entity.IPAddress,
            CurrencyCode = entity.CurrencyCode,
            TimeZone = entity.TimeZone,
            LocationLatitude = entity.LocationLatitude,
            LocationLongitude = entity.LocationLongitude,
            DefaultFiscalPrinterId = entity.DefaultFiscalPrinterId,
            CashierGroupId = entity.CashierGroupId,
            DefaultPaymentTerminalId = entity.DefaultPaymentTerminalId,
        };
    }

    protected override UpdateStorePosDto MapToUpdateDto(StorePosDto entity)
    {
        return new UpdateStorePosDto
        {
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            Location = entity.Location,
            Notes = entity.Notes,
            TerminalIdentifier = entity.TerminalIdentifier,
            IPAddress = entity.IPAddress,
            IsOnline = entity.IsOnline,
            DefaultFiscalPrinterId = entity.DefaultFiscalPrinterId,
            CashierGroupId = entity.CashierGroupId,
            DefaultPaymentTerminalId = entity.DefaultPaymentTerminalId,
        };
    }

    protected override Task<StorePosDto?> CreateEntityAsync(CreateStorePosDto createDto, CancellationToken ct = default)
    {
        return _posService.CreateAsync(createDto, ct);
    }

    protected override Task<StorePosDto?> UpdateEntityAsync(Guid entityId, UpdateStorePosDto updateDto, CancellationToken ct = default)
    {
        return _posService.UpdateAsync(entityId, updateDto, ct);
    }

    protected override Guid GetEntityId(StorePosDto entity)
    {
        return entity.Id;
    }
}
