using Prym.DTOs.PaymentTerminal;

namespace Prym.Web.Services.Store;

public interface IPaymentTerminalService
{
    Task<List<PaymentTerminalDto>> GetAllAsync(CancellationToken ct = default);
    Task<PaymentTerminalDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentTerminalDto?> CreateAsync(CreatePaymentTerminalDto dto, CancellationToken ct = default);
    Task<PaymentTerminalDto?> UpdateAsync(Guid id, UpdatePaymentTerminalDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PaymentResultDto> SendPaymentAsync(Guid terminalId, PaymentRequestDto request, CancellationToken ct = default);
    Task<PaymentResultDto> SendVoidAsync(Guid terminalId, CancellationToken ct = default);
    Task<PaymentResultDto> SendRefundAsync(Guid terminalId, PaymentRequestDto request, CancellationToken ct = default);
    Task TestConnectionAsync(Guid terminalId, CancellationToken ct = default);
}
