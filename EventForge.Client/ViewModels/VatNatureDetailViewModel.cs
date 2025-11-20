using EventForge.Client.Services;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for VatNature detail page
/// </summary>
public class VatNatureDetailViewModel : BaseEntityDetailViewModel<VatNatureDto, CreateVatNatureDto, UpdateVatNatureDto>
{
    private readonly IFinancialService _financialService;

    public VatNatureDetailViewModel(
        IFinancialService financialService,
        ILogger<VatNatureDetailViewModel> logger) 
        : base(logger)
    {
        _financialService = financialService;
    }

    protected override VatNatureDto CreateNewEntity()
    {
        return new VatNatureDto
        {
            Id = Guid.Empty,
            Code = string.Empty,
            Name = string.Empty,
            Description = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null
        };
    }

    protected override async Task<VatNatureDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _financialService.GetVatNatureAsync(entityId);
    }

    protected override CreateVatNatureDto MapToCreateDto(VatNatureDto entity)
    {
        return new CreateVatNatureDto
        {
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description
        };
    }

    protected override UpdateVatNatureDto MapToUpdateDto(VatNatureDto entity)
    {
        return new UpdateVatNatureDto
        {
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description
        };
    }

    protected override Task<VatNatureDto?> CreateEntityAsync(CreateVatNatureDto createDto)
    {
        return _financialService.CreateVatNatureAsync(createDto);
    }

    protected override Task<VatNatureDto?> UpdateEntityAsync(Guid entityId, UpdateVatNatureDto updateDto)
    {
        return _financialService.UpdateVatNatureAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(VatNatureDto entity)
    {
        return entity.Id;
    }
}
