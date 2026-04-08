using EventForge.DTOs.PaymentTerminal;

namespace EventForge.Client.Services.Store;

public interface IPaymentTerminalService
{
    Task<List<PaymentTerminalDto>> GetAllAsync(CancellationToken ct = default);
    Task<PaymentTerminalDto?> GetByIdAsync(Guid id);
    Task<PaymentTerminalDto?> CreateAsync(CreatePaymentTerminalDto dto);
    Task<PaymentTerminalDto?> UpdateAsync(Guid id, UpdatePaymentTerminalDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<PaymentResultDto> SendPaymentAsync(Guid terminalId, PaymentRequestDto request);
    Task<PaymentResultDto> SendVoidAsync(Guid terminalId);
    Task<PaymentResultDto> SendRefundAsync(Guid terminalId, PaymentRequestDto request);
    Task TestConnectionAsync(Guid terminalId);
}
