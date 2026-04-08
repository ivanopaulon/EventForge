using EventForge.Hardware.Interfaces;
using EventForge.Hardware.PrinterProxy;
using System.Net.Http.Json;

namespace EventForge.Server.Services.PaymentTerminal.Communication;

/// <summary>
/// Protocol 17 channel that communicates via UpdateAgent TCP proxy.
/// </summary>
internal sealed class Protocol17AgentChannel : IPaymentTerminalChannel
{
    private readonly HttpClient _httpClient;
    private readonly string _agentBaseUrl;
    private readonly string _host;
    private readonly int _port;
    private readonly int _timeoutMs;

    public bool IsConnected => true;

    public Protocol17AgentChannel(HttpClient httpClient, string agentBaseUrl, string host, int port, int timeoutMs = 30000)
    {
        _httpClient = httpClient;
        _agentBaseUrl = agentBaseUrl.TrimEnd('/');
        _host = host;
        _port = port;
        _timeoutMs = timeoutMs;
    }

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
        var url = $"{_agentBaseUrl}/api/printer-proxy/tcp-test?host={Uri.EscapeDataString(_host)}&port={_port}";
        var response = await _httpClient.GetAsync(url, cts.Token);
        response.EnsureSuccessStatusCode();
    }

    private async Task<Protocol17Response> SendCommandAsync(string command, decimal amount, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeoutMs);

        var requestBytes = Protocol17Protocol.BuildRequest(command, amount);
        var base64Command = Convert.ToBase64String(requestBytes);

        var proxyRequest = new PrinterProxyTcpSendRequest(_host, _port, base64Command);
        var httpResponse = await _httpClient.PostAsJsonAsync(
            $"{_agentBaseUrl}/api/printer-proxy/tcp-send", proxyRequest, cts.Token);
        httpResponse.EnsureSuccessStatusCode();

        var proxyResponse = await httpResponse.Content.ReadFromJsonAsync<PrinterProxySendResponse>(cancellationToken: cts.Token);
        if (proxyResponse?.Response is null)
            return new Protocol17Response(false, "XX", null, 0m, "Empty response from agent proxy");

        var responseBytes = Convert.FromBase64String(proxyResponse.Response);
        var parsed = Protocol17Protocol.Parse(responseBytes);
        return new Protocol17Response(parsed.Approved, parsed.ResponseCode, parsed.AuthorizationCode, parsed.Amount, parsed.ErrorMessage);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
