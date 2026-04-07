using Prym.Client.Services;
using Prym.DTOs.Business;

namespace Prym.Client.ViewModels;

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
            PaymentMethod = Prym.DTOs.Common.PaymentMethod.BankTransfer,
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

    protected override async Task<PaymentTermDto?> CreateEntityAsync(CreatePaymentTermDto createDto)
    {
        return await _financialService.CreatePaymentTermAsync(createDto);
    }

    protected override async Task<PaymentTermDto?> UpdateEntityAsync(Guid entityId, UpdatePaymentTermDto updateDto)
    {
        return await _financialService.UpdatePaymentTermAsync(entityId, updateDto);
    }

    protected override Guid GetEntityId(PaymentTermDto entity)
    {
        return entity.Id;
    }
}
