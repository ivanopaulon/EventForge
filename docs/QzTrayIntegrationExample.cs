// Example usage of the new QZ Tray services
// This can be used in controllers, background services, or any other part of the application

using EventForge.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventForge.Examples;

public class QzTrayIntegrationExample
{
    private readonly QzSigner _signer;
    private readonly QzWebSocketClient _wsClient;
    private readonly ILogger<QzTrayIntegrationExample> _logger;

    public QzTrayIntegrationExample(
        QzSigner signer, 
        QzWebSocketClient wsClient, 
        ILogger<QzTrayIntegrationExample> logger)
    {
        _signer = signer;
        _wsClient = wsClient;
        _logger = logger;
    }

    /// <summary>
    /// Example: Sign a request to find all printers
    /// </summary>
    public async Task<string> SignPrinterDiscoveryRequest()
    {
        var callName = "qz.printers.find";
        var parameters = new object[] { };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Sign the request using SHA512withRSA
        var signature = await _signer.Sign(callName, parameters, timestamp);
        
        _logger.LogInformation("Signed printer discovery request with SHA512withRSA signature");
        return signature;
    }

    /// <summary>
    /// Example: Sign a complex print request
    /// </summary>
    public async Task<string> SignPrintRequest(string printerName, string content)
    {
        var callName = "qz.print";
        var parameters = new object[]
        {
            new
            {
                printer = new { name = printerName },
                options = new { copies = 1, paperThickness = 0.1 }
            },
            new[]
            {
                new
                {
                    type = "raw",
                    format = "plain",
                    data = content
                }
            }
        };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var signature = await _signer.Sign(callName, parameters, timestamp);
        
        _logger.LogInformation("Signed print request for printer {Printer} with SHA512withRSA signature", printerName);
        return signature;
    }

    /// <summary>
    /// Example: Full workflow - Connect, send signed request, disconnect
    /// </summary>
    public async Task<string?> ExecuteSignedQzTrayRequest(string callName, object[] parameters)
    {
        try
        {
            // Connect to QZ Tray
            var connected = await _wsClient.ConnectAsync();
            if (!connected)
            {
                _logger.LogWarning("Failed to connect to QZ Tray");
                return null;
            }

            _logger.LogInformation("Successfully connected to QZ Tray");

            // Send the signed request
            var response = await _wsClient.SendRequestAsync(callName, parameters);
            
            if (response != null)
            {
                _logger.LogInformation("Received response from QZ Tray: {Response}", response);
            }
            else
            {
                _logger.LogWarning("No response received from QZ Tray");
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing signed QZ Tray request");
            return null;
        }
        finally
        {
            // Always close the connection
            await _wsClient.CloseAsync();
            _wsClient.Dispose();
        }
    }

    /// <summary>
    /// Example: Environment variable configuration demonstration
    /// </summary>
    public static void DemonstrateEnvironmentConfiguration()
    {
        // These environment variables can be set to customize the QZ Tray integration:
        
        // Custom private key path
        Environment.SetEnvironmentVariable("QZ_PRIVATE_KEY_PATH", "/custom/path/to/private-key.pem");
        
        // Custom QZ Tray WebSocket URI
        Environment.SetEnvironmentVariable("QZ_WS_URI", "ws://custom-host:9999");
        
        // After setting these variables, the services will automatically use them
        Console.WriteLine("Environment variables set for QZ Tray integration:");
        Console.WriteLine($"Private Key Path: {Environment.GetEnvironmentVariable("QZ_PRIVATE_KEY_PATH")}");
        Console.WriteLine($"WebSocket URI: {Environment.GetEnvironmentVariable("QZ_WS_URI")}");
    }
}

// Example of how to use this in a controller or service
public class ExampleController : ControllerBase
{
    private readonly QzTrayIntegrationExample _qzExample;

    public ExampleController(QzTrayIntegrationExample qzExample)
    {
        _qzExample = qzExample;
    }

    [HttpPost("example/print")]
    public async Task<IActionResult> ExamplePrint([FromBody] PrintRequest request)
    {
        var response = await _qzExample.ExecuteSignedQzTrayRequest(
            "qz.print", 
            new object[] { request.PrinterConfig, request.Data });

        if (response != null)
        {
            return Ok(new { success = true, response });
        }
        
        return BadRequest(new { success = false, message = "Failed to execute QZ Tray request" });
    }
}

public class PrintRequest
{
    public object PrinterConfig { get; set; } = new { };
    public object[] Data { get; set; } = Array.Empty<object>();
}