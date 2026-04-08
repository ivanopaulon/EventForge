namespace EventForge.UpdateAgent.Services;

/// <summary>
/// Abstraction for communicating with printers on the local machine or local network.
/// Supports USB-attached printers (raw byte I/O via device path) and TCP/IP network printers
/// (raw byte I/O via TCP socket), as well as generic HTTP forwarding for WebAPI-based printers
/// (e.g. Epson ePOS-Print).
/// </summary>
public interface IAgentPrinterService
{
    /// <summary>
    /// Sends a raw command to a USB printer identified by <paramref name="deviceId"/>
    /// and returns the printer's response bytes.
    /// </summary>
    /// <param name="deviceId">
    /// The Windows device path suffix, e.g. <c>USB001</c> (resolved to <c>\\.\USB001</c>).
    /// </param>
    /// <param name="command">Raw command bytes to write to the device.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response bytes read from the device after writing the command.</returns>
    Task<byte[]> SendCommandAsync(string deviceId, byte[] command, CancellationToken ct = default);

    /// <summary>
    /// Verifies that the device at <paramref name="deviceId"/> can be opened for writing.
    /// Throws <see cref="InvalidOperationException"/> when the device is not accessible.
    /// </summary>
    /// <param name="deviceId">The Windows device path suffix, e.g. <c>USB001</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task TestConnectionAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Enumerates USB printer device paths that exist on the local machine
    /// (probes <c>\\.\USB001</c> through <c>\\.\USB009</c>).
    /// </summary>
    /// <returns>Read-only list of accessible device path suffixes (e.g. <c>USB001</c>).</returns>
    IReadOnlyList<string> ListDevices();

    // ── TCP network printer (TcpViaAgent) ─────────────────────────────────────

    /// <summary>
    /// Opens a TCP connection to a network printer at <paramref name="host"/>:<paramref name="port"/>
    /// on the agent's local network, sends the raw <paramref name="command"/> bytes, and returns
    /// the response bytes. Used when the printer's IP is reachable from the agent but not from the server.
    /// </summary>
    /// <param name="host">Printer IP address or hostname on the agent's local network.</param>
    /// <param name="port">TCP port on the printer (typically 9100).</param>
    /// <param name="command">Raw command bytes (ESC/POS, Custom protocol, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response bytes read from the printer.</returns>
    Task<byte[]> SendTcpCommandAsync(string host, int port, byte[] command, CancellationToken ct = default);

    /// <summary>
    /// Verifies TCP connectivity to a network printer on the agent's local network.
    /// Throws <see cref="InvalidOperationException"/> when the printer cannot be reached.
    /// </summary>
    /// <param name="host">Printer IP address or hostname.</param>
    /// <param name="port">TCP port on the printer.</param>
    /// <param name="ct">Cancellation token.</param>
    Task TestTcpConnectionAsync(string host, int port, CancellationToken ct = default);

    // ── HTTP forward (TcpViaAgent – Epson WebAPI and similar) ─────────────────

    /// <summary>
    /// Forwards an HTTP POST request to <paramref name="targetUrl"/> on the agent's local network
    /// and returns the response body. Used to proxy Epson ePOS-Print XML (and similar WebAPI
    /// protocols) through the agent when the printer is not reachable from the server.
    /// </summary>
    /// <param name="targetUrl">Full URL of the printer's HTTP endpoint (e.g. <c>http://192.168.1.100:80/api/1/request</c>).</param>
    /// <param name="contentType">MIME type for the request body (e.g. <c>text/xml; charset=utf-8</c>).</param>
    /// <param name="body">Request body string (XML, JSON, etc.).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Raw response body string returned by the printer's HTTP server.</returns>
    Task<string> ForwardHttpAsync(string targetUrl, string contentType, string body, CancellationToken ct = default);
}
