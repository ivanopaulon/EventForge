using EventForge.Hardware.Interfaces;
using EventForge.Hardware.PrinterProxy;
using System.Net.Http.Json;

namespace EventForge.Server.Services.PaymentTerminal.Communication;

/// <summary>
/// Protocol 17 channel that routes TCP communication through an UpdateAgent proxy.
/// Uses the agent's existing <c>/api/printer-proxy/tcp-send</c> and <c>/api/printer-proxy/tcp-test</c> endpoints.
/// </summary>
internal sealed class Protocol17AgentChannel(
    HttpClient httpClient,
    string agentBaseUrl,
    string host,
    int port,
    int timeoutMs = 30000) : IPaymentTerminalChannel
{
    private readonly string _agentBaseUrl = agentBaseUrl.TrimEnd('/');

    /// <inheritdoc />
    /// <remarks>Always <see langword="true"/> — HTTP is stateless, tested per-request.</remarks>
    public bool IsConnected => true;

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
        var url = $"{_agentBaseUrl}/api/printer-proxy/tcp-test?host={Uri.EscapeDataString(host)}&port={port}";
        var response = await httpClient.GetAsync(url, cts.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Agent TCP test failed for {host}:{port} (HTTP {(int)response.StatusCode}): {body}");
        }
    }

    private async Task<Protocol17Response> SendCommandAsync(string command, decimal amount, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);

        var requestBytes = Protocol17Protocol.BuildRequest(command, amount);
        var proxyRequest = new PrinterProxyTcpSendRequest(host, port, Convert.ToBase64String(requestBytes));

        var httpResponse = await httpClient
            .PostAsJsonAsync($"{_agentBaseUrl}/api/printer-proxy/tcp-send", proxyRequest, cts.Token)
            .ConfigureAwait(false);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var body = await httpResponse.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Agent TCP send failed for {host}:{port} (HTTP {(int)httpResponse.StatusCode}): {body}");
        }

        var proxyResponse = await httpResponse.Content
            .ReadFromJsonAsync<PrinterProxySendResponse>(cancellationToken: cts.Token)
            .ConfigureAwait(false);

        if (proxyResponse?.Response is null)
            return new Protocol17Response(false, "XX", null, 0m, "Risposta vuota dall'agente proxy.");

        var responseBytes = Convert.FromBase64String(proxyResponse.Response);
        var parsed = Protocol17Protocol.Parse(responseBytes);
        return new Protocol17Response(parsed.Approved, parsed.ResponseCode, parsed.AuthorizationCode, parsed.Amount, parsed.ErrorMessage);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
