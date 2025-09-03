# QZ Tray Integration with SHA512withRSA Signatures

This implementation provides QZ Tray integration with digital signatures using SHA512withRSA for enhanced security.

## Features

- **QzSigner**: Signs QZ Tray requests using SHA512withRSA with PKCS#1 v1.5 padding
- **QzWebSocketClient**: Manages WebSocket connections to QZ Tray with certificate authentication
- **Environment Variable Support**: Configurable paths and URIs through environment variables
- **Maintains Existing Files**: Preserves existing `private-key.pem` and `digital-certificate.txt` files

## Environment Variables

The services support the following environment variables:

- `QZ_PRIVATE_KEY_PATH`: Path to the private key file (defaults to `private-key.pem`)
- `QZ_WS_URI`: QZ Tray WebSocket URI (defaults to `ws://localhost:8181`)

## Usage Examples

### Basic Signing

```csharp
// Inject the QzSigner service
var signer = serviceProvider.GetRequiredService<QzSigner>();

// Sign a QZ Tray request
var callName = "qz.printers.find";
var parameters = new object[] { };
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

var signature = await signer.Sign(callName, parameters, timestamp);
```

### WebSocket Communication

```csharp
// Inject the QzWebSocketClient service
var wsClient = serviceProvider.GetRequiredService<QzWebSocketClient>();

// Connect to QZ Tray
var connected = await wsClient.ConnectAsync();

if (connected)
{
    // Send a signed request
    var response = await wsClient.SendRequestAsync(
        "qz.printers.find", 
        new object[] { }, 
        cancellationToken);
    
    // Process response
    Console.WriteLine($"QZ Response: {response}");
}

// Clean up
await wsClient.CloseAsync();
wsClient.Dispose();
```

### Environment Variable Configuration

```bash
# Set custom paths
export QZ_PRIVATE_KEY_PATH="/path/to/custom/private-key.pem"
export QZ_WS_URI="ws://custom-host:9999"
```

## JSON Signature Format

The QzSigner creates signatures for JSON payloads with properties in this specific order:

1. `call` - The QZ function name
2. `params` - Function parameters array
3. `timestamp` - Unix timestamp in milliseconds

Example JSON:
```json
{
  "call": "qz.printers.find",
  "params": [],
  "timestamp": 1672531200000
}
```

## Security Features

- **SHA512withRSA**: Uses SHA-512 hashing with RSA signature (stronger than SHA-256)
- **PKCS#1 v1.5**: Standard signature padding scheme
- **PEM Key Support**: Supports both PKCS#8 and PKCS#1 PEM format keys
- **Certificate Chain**: Automatic certificate loading for QZ Tray authentication

## Integration Notes

- Services are automatically registered in the DI container
- Compatible with existing QZ Tray infrastructure
- Maintains backward compatibility
- Thread-safe operations
- Proper error handling and logging