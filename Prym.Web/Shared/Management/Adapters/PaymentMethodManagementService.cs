using Prym.Web.Services.Sales;
using Prym.DTOs.Common;
using Prym.DTOs.Sales;

namespace Prym.Web.Shared.Management.Adapters;

public class PaymentMethodManagementService : IEntityManagementService<PaymentMethodDto>
{
    private readonly IPaymentMethodService _paymentMethodService;

    public PaymentMethodManagementService(IPaymentMethodService paymentMethodService)
        => _paymentMethodService = paymentMethodService;

    public async Task<PagedResult<PaymentMethodDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        // searchTerm and filters are intentionally ignored here because UseServerSidePaging=false;
        // filtering and search are handled client-side by EntityManagementPage.
        => await _paymentMethodService.GetPagedAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _paymentMethodService.DeleteAsync(id);
}
