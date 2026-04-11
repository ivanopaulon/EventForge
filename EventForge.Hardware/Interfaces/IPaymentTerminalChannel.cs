namespace Prym.Hardware.Interfaces;

/// <summary>
/// Abstraction for Protocol 17 (ECR17) TCP communication with a POS payment terminal.
/// </summary>
public interface IPaymentTerminalChannel : IAsyncDisposable
{
    Task<Protocol17Response> SendPaymentAsync(decimal amountEur, CancellationToken ct = default);
    Task<Protocol17Response> SendVoidAsync(CancellationToken ct = default);
    Task<Protocol17Response> SendRefundAsync(decimal amountEur, CancellationToken ct = default);
    Task TestConnectionAsync(CancellationToken ct = default);
    bool IsConnected { get; }
}
