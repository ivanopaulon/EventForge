using EventForge.Client.Services.Store;
using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Client.ViewModels;

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

    protected override Task<StorePosDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return _posService.GetByIdAsync(entityId);
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
            LocationLongitude = entity.LocationLongitude
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
            IsOnline = entity.IsOnline
        };
    }

    protected override Task<StorePosDto?> CreateEntityAsync(CreateStorePosDto createDto)
    {
        return _posService.CreateAsync(createDto);
    }

    protected override Task<StorePosDto?> UpdateEntityAsync(Guid entityId, UpdateStorePosDto updateDto)
    {
        return _posService.UpdateAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(StorePosDto entity)
    {
        return entity.Id;
    }
}
