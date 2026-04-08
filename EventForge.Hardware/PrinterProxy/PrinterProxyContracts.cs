using System.Text.Json.Serialization;

namespace EventForge.Hardware.PrinterProxy;

// ── USB proxy (UsbViaAgent) ──────────────────────────────────────────────────

/// <summary>
/// Request body for <c>POST /api/printer-proxy/send</c> (USB via Agent).
/// </summary>
/// <param name="DeviceId">USB device path suffix, e.g. <c>USB001</c> (resolved to <c>\\.\USB001</c>).</param>
/// <param name="Command">Base64-encoded raw command bytes to write to the device.</param>
public sealed record PrinterProxySendRequest(
    [property: JsonPropertyName("deviceId")] string DeviceId,
    [property: JsonPropertyName("command")] string Command);

/// <summary>
/// Response body for <c>POST /api/printer-proxy/send</c> (USB via Agent) and
/// <c>POST /api/printer-proxy/tcp-send</c> (TCP via Agent).
/// </summary>
/// <param name="Response">Base64-encoded raw response bytes read from the device/printer.</param>
public sealed record PrinterProxySendResponse(
    [property: JsonPropertyName("response")] string Response);

// ── TCP proxy (TcpViaAgent) ──────────────────────────────────────────────────

/// <summary>
/// Request body for <c>POST /api/printer-proxy/tcp-send</c> (TCP network printer via Agent).
/// Instructs the agent to open a TCP connection to a printer on its local network,
/// send the raw command bytes, and return the response.
/// </summary>
/// <param name="Host">Printer IP address or hostname on the agent's local network.</param>
/// <param name="Port">TCP port on the printer (typically 9100).</param>
/// <param name="Command">Base64-encoded raw command bytes.</param>
public sealed record PrinterProxyTcpSendRequest(
    [property: JsonPropertyName("host")] string Host,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("command")] string Command);

/// <summary>
/// Request body for <c>GET /api/printer-proxy/tcp-test</c>.
/// Tests TCP connectivity to a printer on the agent's local network.
/// Passed as query parameters, not a JSON body.
/// </summary>
/// <param name="Host">Printer IP address or hostname.</param>
/// <param name="Port">TCP port on the printer.</param>
public sealed record PrinterProxyTcpTestRequest(string Host, int Port);

// ── HTTP forward (TcpViaAgent – Epson WebAPI, etc.) ─────────────────────────

/// <summary>
/// Request body for <c>POST /api/printer-proxy/http-forward</c> (Epson WebAPI via Agent).
/// Instructs the agent to forward an HTTP POST request to a printer's WebAPI endpoint
/// (e.g. Epson ePOS-Print XML) on its local network and return the response body.
/// </summary>
/// <param name="TargetUrl">Full URL of the printer's HTTP endpoint (e.g. <c>http://192.168.1.100:80/api/1/request</c>).</param>
/// <param name="ContentType">MIME type of the request body (e.g. <c>text/xml; charset=utf-8</c>).</param>
/// <param name="Body">Request body string (XML, JSON, etc.).</param>
public sealed record PrinterProxyHttpForwardRequest(
    [property: JsonPropertyName("targetUrl")] string TargetUrl,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("body")] string Body);

/// <summary>
/// Response body for <c>POST /api/printer-proxy/http-forward</c>.
/// </summary>
/// <param name="ResponseBody">Raw response body returned by the printer's HTTP server.</param>
public sealed record PrinterProxyHttpForwardResponse(
    [property: JsonPropertyName("responseBody")] string ResponseBody);
