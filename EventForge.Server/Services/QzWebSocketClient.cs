using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EventForge.Server.Services;

/// <summary>
/// WebSocket client for communicating with QZ Tray
/// </summary>
public class QzWebSocketClient : IDisposable
{
    private readonly ILogger<QzWebSocketClient> _logger;
    private readonly QzSigner _signer;
    private readonly string _wsUri;
    private readonly string _certificatePath;
    private ClientWebSocket? _webSocket;
    private bool _disposed = false;

    public QzWebSocketClient(ILogger<QzWebSocketClient> logger, QzSigner signer, IConfiguration configuration)
    {
        _logger = logger;
        _signer = signer;
        _wsUri = Environment.GetEnvironmentVariable("QZ_WS_URI") ?? "ws://localhost:8181";
        _certificatePath = "digital-certificate.txt";
    }

    /// <summary>
    /// Connects to QZ Tray WebSocket and sends initial certificate message
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection and certificate sending succeeded</returns>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _webSocket = new ClientWebSocket();
            var uri = new Uri(_wsUri);
            
            _logger.LogInformation("Connecting to QZ Tray at {Uri}", _wsUri);
            await _webSocket.ConnectAsync(uri, cancellationToken);

            if (_webSocket.State == WebSocketState.Open)
            {
                _logger.LogInformation("Successfully connected to QZ Tray");
                await SendCertificateAsync(cancellationToken);
                return true;
            }

            _logger.LogWarning("Failed to connect to QZ Tray - WebSocket state: {State}", _webSocket.State);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to QZ Tray at {Uri}", _wsUri);
            return false;
        }
    }

    /// <summary>
    /// Sends a signed request to QZ Tray
    /// </summary>
    /// <param name="callName">QZ Tray function name</param>
    /// <param name="parameters">Function parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from QZ Tray</returns>
    public async Task<string?> SendRequestAsync(string callName, object[] parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_webSocket?.State != WebSocketState.Open)
            {
                _logger.LogWarning("WebSocket is not open. Current state: {State}", _webSocket?.State);
                return null;
            }

            // Create timestamp
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Sign the request
            var signature = await _signer.Sign(callName, parameters, timestamp);

            // Create the request message
            var request = new
            {
                call = callName,
                @params = parameters,
                timestamp = timestamp,
                signature = signature
            };

            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = false });
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);

            _logger.LogDebug("Sending signed request to QZ Tray: {Call}", callName);

            // Send the request
            await _webSocket.SendAsync(
                new ArraySegment<byte>(requestBytes), 
                WebSocketMessageType.Text, 
                true, 
                cancellationToken);

            // Receive response
            var responseBuffer = new byte[8192];
            var response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);

            if (response.MessageType == WebSocketMessageType.Text)
            {
                var responseText = Encoding.UTF8.GetString(responseBuffer, 0, response.Count);
                _logger.LogDebug("Received response from QZ Tray: {Response}", responseText);
                return responseText;
            }

            _logger.LogWarning("Received non-text response from QZ Tray: {MessageType}", response.MessageType);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request to QZ Tray");
            return null;
        }
    }

    private async Task SendCertificateAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Load certificate
            var certificateContent = await LoadCertificateAsync();
            
            // Create certificate message
            var certificateMessage = new
            {
                certificate = certificateContent
            };

            var messageJson = JsonSerializer.Serialize(certificateMessage, new JsonSerializerOptions { WriteIndented = false });
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            _logger.LogDebug("Sending certificate to QZ Tray");

            await _webSocket!.SendAsync(
                new ArraySegment<byte>(messageBytes), 
                WebSocketMessageType.Text, 
                true, 
                cancellationToken);

            _logger.LogInformation("Certificate sent to QZ Tray successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending certificate to QZ Tray");
            throw;
        }
    }

    private async Task<string> LoadCertificateAsync()
    {
        try
        {
            var resolvedPath = Path.IsPathRooted(_certificatePath) 
                ? _certificatePath 
                : Path.Combine(AppContext.BaseDirectory, _certificatePath);

            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException($"Certificate file not found: {resolvedPath}");
            }

            var certificate = await File.ReadAllTextAsync(resolvedPath);
            _logger.LogDebug("Successfully loaded certificate from {Path}", resolvedPath);
            return certificate.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading certificate from {Path}", _certificatePath);
            throw;
        }
    }

    /// <summary>
    /// Closes the WebSocket connection
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", cancellationToken);
                _logger.LogInformation("QZ Tray WebSocket connection closed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing QZ Tray WebSocket connection");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _webSocket?.Dispose();
            _disposed = true;
        }
    }
}