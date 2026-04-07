using EventForge.Client.Services.Station;
using EventForge.DTOs.Common;
using EventForge.DTOs.Station;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for Station detail dialog following the PosDetail pattern.
/// </summary>
public class StationDetailViewModel : BaseEntityDetailViewModel<StationDto, CreateStationDto, UpdateStationDto>
{
    private readonly IStationService _stationService;

    public StationDetailViewModel(
        IStationService stationService,
        ILogger<StationDetailViewModel> logger)
        : base(logger)
    {
        _stationService = stationService;
    }

    protected override StationDto CreateNewEntity()
    {
        return new StationDto
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Status = StationStatus.Active,
            StationType = StationType.KDS
        };
    }

    protected override Task<StationDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return _stationService.GetByIdAsync(entityId);
    }

    protected override CreateStationDto MapToCreateDto(StationDto entity)
    {
        return new CreateStationDto
        {
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            Location = entity.Location,
            SortOrder = entity.SortOrder,
            Notes = entity.Notes,
            StationType = entity.StationType,
            AssignedPrinterId = entity.AssignedPrinterId,
            PrintsReceiptCopy = entity.PrintsReceiptCopy
        };
    }

    protected override UpdateStationDto MapToUpdateDto(StationDto entity)
    {
        return new UpdateStationDto
        {
            Name = entity.Name,
            Description = entity.Description,
            Status = entity.Status,
            Location = entity.Location,
            SortOrder = entity.SortOrder,
            Notes = entity.Notes,
            StationType = entity.StationType,
            AssignedPrinterId = entity.AssignedPrinterId,
            PrintsReceiptCopy = entity.PrintsReceiptCopy
        };
    }

    protected override Task<StationDto?> CreateEntityAsync(CreateStationDto createDto)
    {
        return _stationService.CreateAsync(createDto);
    }

    protected override Task<StationDto?> UpdateEntityAsync(Guid entityId, UpdateStationDto updateDto)
    {
        return _stationService.UpdateAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(StationDto entity)
    {
        return entity.Id;
    }
}
