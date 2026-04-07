using System.Text.Json.Serialization;

namespace Prym.Agent.Models;

/// <summary>Request body for POST /api/printer-proxy/send.</summary>
/// <param name="DeviceId">USB device path suffix (e.g. <c>USB001</c>).</param>
/// <param name="Command">Base64-encoded raw command bytes.</param>
public sealed record PrinterProxyRequest(
    [property: JsonPropertyName("deviceId")] string DeviceId,
    [property: JsonPropertyName("command")] string Command);

/// <summary>Response body for POST /api/printer-proxy/send.</summary>
/// <param name="Response">Base64-encoded raw response bytes from the printer.</param>
public sealed record PrinterProxyResponse(
    [property: JsonPropertyName("response")] string Response);
