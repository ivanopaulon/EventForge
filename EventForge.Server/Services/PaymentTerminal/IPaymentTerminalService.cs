using Prym.DTOs.PaymentTerminal;

namespace EventForge.Server.Services.PaymentTerminal;

public interface IPaymentTerminalService
{
    Task<List<PaymentTerminalDto>> GetAllAsync(CancellationToken ct = default);
    Task<PaymentTerminalDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentTerminalDto> CreateAsync(CreatePaymentTerminalDto dto, string currentUser, CancellationToken ct = default);
    Task<PaymentTerminalDto?> UpdateAsync(Guid id, UpdatePaymentTerminalDto dto, string currentUser, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken ct = default);
    Task<PaymentResultDto> SendPaymentAsync(Guid terminalId, PaymentRequestDto request, CancellationToken ct = default);
    Task<PaymentResultDto> SendVoidAsync(Guid terminalId, CancellationToken ct = default);
    Task<PaymentResultDto> SendRefundAsync(Guid terminalId, PaymentRequestDto request, CancellationToken ct = default);
    Task TestConnectionAsync(Guid terminalId, CancellationToken ct = default);
    Task TestTcpConnectionAsync(string host, int port, int timeoutMs, CancellationToken ct = default);
    Task TestTcpViaAgentAsync(string agentBaseUrl, string host, int port, int timeoutMs, CancellationToken ct = default);
}
