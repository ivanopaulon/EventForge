using Prym.Hardware.Interfaces;
using System.Net.Sockets;

namespace EventForge.Server.Services.PaymentTerminal.Communication;

/// <summary>
/// Protocol 17 channel that connects directly via TCP from the server to the payment terminal.
/// Opens a fresh connection for every operation — payment terminals do not maintain persistent sessions.
/// </summary>
internal sealed class Protocol17TcpChannel(string host, int port, int timeoutMs = 30000) : IPaymentTerminalChannel
{
    /// <inheritdoc />
    /// <remarks>
    /// Always returns <see langword="false"/> because this implementation is stateless
    /// (a new TCP connection is opened per request).
    /// </remarks>
    public bool IsConnected => false;

    /// <inheritdoc />
    public Task<Protocol17Response> SendPaymentAsync(decimal amountEur, CancellationToken ct = default)
        => SendCommandAsync(Protocol17Protocol.CmdPayment, amountEur, ct);

    /// <inheritdoc />
    public Task<Protocol17Response> SendVoidAsync(CancellationToken ct = default)
        => SendCommandAsync(Protocol17Protocol.CmdVoid, 0m, ct);

    /// <inheritdoc />
    public Task<Protocol17Response> SendRefundAsync(decimal amountEur, CancellationToken ct = default)
        => SendCommandAsync(Protocol17Protocol.CmdRefund, amountEur, ct);

    /// <inheritdoc />
    public async Task TestConnectionAsync(CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
    }

    private async Task<Protocol17Response> SendCommandAsync(string command, decimal amount, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);

        // Open a fresh TCP connection per request — POS terminals are stateless
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);

        using var stream = client.GetStream();
        var requestBytes = Protocol17Protocol.BuildRequest(command, amount);
        await stream.WriteAsync(requestBytes, cts.Token).ConfigureAwait(false);
        await stream.FlushAsync(cts.Token).ConfigureAwait(false);

        // Read until full frame (STX … ETX BCC) is received — TCP may split data across reads
        var buffer = new byte[256];
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(totalRead), cts.Token).ConfigureAwait(false);
            if (read == 0) break; // connection closed
            totalRead += read;

            // Stop once we have at least ETX + BCC (ETX=0x03 at some position, with one more byte after)
            int etxPos = Array.IndexOf(buffer, (byte)0x03, 0, totalRead);
            if (etxPos >= 0 && totalRead > etxPos + 1) break;
        }

        var parsed = Protocol17Protocol.Parse(buffer.AsSpan(0, totalRead).ToArray());
        return new Protocol17Response(parsed.Approved, parsed.ResponseCode, parsed.AuthorizationCode, parsed.Amount, parsed.ErrorMessage);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
