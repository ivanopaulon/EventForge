using EventForge.Client.Services;
using EventForge.DTOs.Business;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for PaymentTerm detail page
/// </summary>
public class PaymentTermDetailViewModel : BaseEntityDetailViewModel<PaymentTermDto, CreatePaymentTermDto, UpdatePaymentTermDto>
{
    private readonly IFinancialService _financialService;

    public PaymentTermDetailViewModel(
        IFinancialService financialService,
        ILogger<PaymentTermDetailViewModel> logger)
        : base(logger)
    {
        _financialService = financialService;
    }

    protected override PaymentTermDto CreateNewEntity()
    {
        return new PaymentTermDto
        {
            Id = Guid.Empty,
            Name = string.Empty,
            Description = null,
            DueDays = 0,
            PaymentMethod = EventForge.DTOs.Common.PaymentMethod.BankTransfer,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = null,
            ModifiedAt = null,
            ModifiedBy = null
        };
    }

    protected override async Task<PaymentTermDto?> LoadEntityFromServiceAsync(Guid entityId)
    {
        return await _financialService.GetPaymentTermAsync(entityId);
    }

    protected override async Task LoadRelatedEntitiesAsync(Guid entityId)
    {
        // PaymentTerm is standalone - no related entities
        await Task.CompletedTask;
    }

    protected override CreatePaymentTermDto MapToCreateDto(PaymentTermDto entity)
    {
        return new CreatePaymentTermDto
        {
            Name = entity.Name,
            Description = entity.Description,
            DueDays = entity.DueDays,
            PaymentMethod = entity.PaymentMethod
        };
    }

    protected override UpdatePaymentTermDto MapToUpdateDto(PaymentTermDto entity)
    {
        return new UpdatePaymentTermDto
        {
            Name = entity.Name,
            Description = entity.Description,
            DueDays = entity.DueDays,
            PaymentMethod = entity.PaymentMethod
        };
    }

    protected override Task<PaymentTermDto?> CreateEntityAsync(CreatePaymentTermDto createDto)
    {
        return _financialService.CreatePaymentTermAsync(createDto);
    }

    protected override Task<PaymentTermDto?> UpdateEntityAsync(Guid entityId, UpdatePaymentTermDto updateDto)
    {
        return _financialService.UpdatePaymentTermAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(PaymentTermDto entity)
    {
        return entity.Id;
    }
}
