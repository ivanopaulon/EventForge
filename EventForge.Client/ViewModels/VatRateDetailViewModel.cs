using EventForge.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.VatRates;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for VatRate detail page
/// </summary>
public class VatRateDetailViewModel : BaseEntityDetailViewModel<VatRateDto, CreateVatRateDto, UpdateVatRateDto>
{
    private readonly IFinancialService _financialService;

    public VatRateDetailViewModel(
        IFinancialService financialService,
        ILogger<VatRateDetailViewModel> logger)
        : base(logger)
    {
        _financialService = financialService;
    }

    // Related entity collections
    public IEnumerable<VatNatureDto>? VatNatures { get; private set; }

    protected override VatRateDto CreateNewEntity()
    {
        return new VatRateDto
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Percentage = 0m,
            Status = VatRateStatus.Active,
            ValidFrom = null,
            ValidTo = null,
            Notes = null,
            VatNatureId = null,
            VatNatureCode = null,
            VatNatureName = null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null
        };
    }

    protected override async Task<VatRateDto?> LoadEntityFromServiceAsync(Guid entityId, CancellationToken ct = default)
    {
        return await _financialService.GetVatRateAsync(entityId, ct);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId, CancellationToken ct = default)
    {
        if (IsNewEntity)
        {
            VatNatures = new List<VatNatureDto>();
            return;
        }

        try
        {
            // Load VAT natures for dropdown
            var vatNaturesResult = await _financialService.GetVatNaturesAsync(1, 100, ct);
            VatNatures = vatNaturesResult?.Items ?? new List<VatNatureDto>();

            Logger.LogInformation("Loaded {Count} VAT natures for VAT rate {Id}",
                VatNatures.Count(), entityId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading related entities for VAT rate {Id}", entityId);
            VatNatures = new List<VatNatureDto>();
        }
    }

    protected override CreateVatRateDto MapToCreateDto(VatRateDto entity)
    {
        return new CreateVatRateDto
        {
            Name = entity.Name,
            Percentage = entity.Percentage,
            Status = entity.Status,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Notes = entity.Notes,
            VatNatureId = entity.VatNatureId
        };
    }

    protected override UpdateVatRateDto MapToUpdateDto(VatRateDto entity)
    {
        return new UpdateVatRateDto
        {
            Name = entity.Name,
            Percentage = entity.Percentage,
            Status = entity.Status,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Notes = entity.Notes,
            VatNatureId = entity.VatNatureId
        };
    }

    protected override async Task<VatRateDto?> CreateEntityAsync(CreateVatRateDto createDto, CancellationToken ct = default)
    {
        return await _financialService.CreateVatRateAsync(createDto, ct);
    }

    protected override async Task<VatRateDto?> UpdateEntityAsync(Guid entityId, UpdateVatRateDto updateDto, CancellationToken ct = default)
    {
        return await _financialService.UpdateVatRateAsync(entityId, updateDto, ct);
    }

    protected override Guid GetEntityId(VatRateDto entity)
    {
        return entity.Id;
    }
}
