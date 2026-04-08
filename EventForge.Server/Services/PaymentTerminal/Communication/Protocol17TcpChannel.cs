using EventForge.Hardware.Interfaces;
using System.Net.Sockets;

namespace EventForge.Server.Services.PaymentTerminal.Communication;

/// <summary>
/// Protocol 17 channel that connects directly via TCP from the server.
/// </summary>
internal sealed class Protocol17TcpChannel : IPaymentTerminalChannel
{
    private readonly string _host;
    private readonly int _port;
    private readonly int _timeoutMs;
    private TcpClient? _tcpClient;
    private bool _disposed;

    public Protocol17TcpChannel(string host, int port, int timeoutMs = 30000)
    {
        _host = host;
        _port = port;
        _timeoutMs = timeoutMs;
    }

    public bool IsConnected => _tcpClient?.Connected == true;

    public async Task<Protocol17Response> SendPaymentAsync(decimal amountEur, CancellationToken ct = default)
        => await SendCommandAsync(Protocol17Protocol.CmdPayment, amountEur, ct);

    public async Task<Protocol17Response> SendVoidAsync(CancellationToken ct = default)
        => await SendCommandAsync(Protocol17Protocol.CmdVoid, 0m, ct);

    public async Task<Protocol17Response> SendRefundAsync(decimal amountEur, CancellationToken ct = default)
        => await SendCommandAsync(Protocol17Protocol.CmdRefund, amountEur, ct);

    public async Task TestConnectionAsync(CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeoutMs);
        var client = new TcpClient();
        await client.ConnectAsync(_host, _port, cts.Token);
        client.Close();
    }

    private async Task<Protocol17Response> SendCommandAsync(string command, decimal amount, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeoutMs);

        _tcpClient ??= new TcpClient();
        if (!_tcpClient.Connected)
            await _tcpClient.ConnectAsync(_host, _port, cts.Token);

        var stream = _tcpClient.GetStream();
        var request = Protocol17Protocol.BuildRequest(command, amount);
        await stream.WriteAsync(request, cts.Token);
        await stream.FlushAsync(cts.Token);

        var buffer = new byte[256];
        int read = await stream.ReadAsync(buffer, cts.Token);
        var response = buffer[..read];

        var parsed = Protocol17Protocol.Parse(response);
        return new Protocol17Response(parsed.Approved, parsed.ResponseCode, parsed.AuthorizationCode, parsed.Amount, parsed.ErrorMessage);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (_tcpClient is not null)
            {
                _tcpClient.Close();
                _tcpClient.Dispose();
                _tcpClient = null;
            }
        }
        await ValueTask.CompletedTask;
    }
}
