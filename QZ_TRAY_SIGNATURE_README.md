# QZ Tray Digital Signature Implementation

This document describes the implementation of digital signature functionality for QZ Tray print requests in EventForge.

## Overview

The QZ Tray printing service now supports RSA-SHA256 digital signatures for all print requests. This ensures that QZ Tray accepts the print requests securely and prevents unauthorized printing operations.

## Implementation Details

### Components

1. **QzDigitalSignatureService**: Handles the creation of RSA-SHA256 digital signatures
2. **Modified QzPrintingService**: Updated to sign all print payloads before sending to QZ Tray
3. **Certificate and Private Key Files**: Required for signature generation

### Files Added/Modified

- `EventForge.Server/Services/Printing/QzDigitalSignatureService.cs` - New service for digital signatures
- `EventForge.Server/Services/Printing/QzPrintingService.cs` - Modified to use digital signatures
- `EventForge.Server/Services/Interfaces/IQzPrintingService.cs` - Added signature validation method
- `EventForge.Server/Extensions/ServiceCollectionExtensions.cs` - Registered signature service
- `EventForge.Server/appsettings.json` - Added QZ signing configuration
- `EventForge.Server/private-key.pem` - RSA private key for signing
- `EventForge.Server/digital-certificate.txt` - X.509 certificate for verification

### Configuration

Add the following configuration to `appsettings.json`:

```json
{
  "QzSigning": {
    "PrivateKeyPath": "private-key.pem",
    "CertificatePath": "digital-certificate.txt"
  }
}
```

### How It Works

1. When a print job is submitted via `SubmitPrintJobAsync`, the service creates the standard QZ Tray payload
2. The payload is passed to `QzDigitalSignatureService.SignPayloadAsync()` 
3. The service:
   - Serializes the payload to JSON
   - Loads the RSA private key from the PEM file
   - Creates an RSA-SHA256 signature of the JSON payload
   - Loads the X.509 certificate in PEM format
   - Returns a new payload with `signature` and `certificate` fields added
4. The signed payload is sent to QZ Tray via WebSocket

### Signed Payload Format

The signed payload sent to QZ Tray has this structure:

```json
{
  "call": "qz.print",
  "params": [
    {
      "printer": "PrinterName",
      "data": [
        {
          "type": "raw",
          "data": "Print content"
        }
      ]
    }
  ],
  "signature": "<Base64-encoded RSA-SHA256 signature>",
  "certificate": "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----"
}
```

## Security Considerations

### Certificate Management

- The private key (`private-key.pem`) should be kept secure and not committed to public repositories
- The certificate (`digital-certificate.txt`) contains the public key and can be shared
- In production, use proper certificate management and rotation policies

### File Permissions

- Ensure the private key file has restricted permissions (600 on Unix systems)
- Consider storing certificates in a secure key management system for production

## Testing

A test implementation is available that:

1. Validates the signature configuration
2. Signs a sample QZ Tray payload
3. Verifies the signed payload structure

The test confirms that:
- ✓ Private key and certificate files are accessible
- ✓ RSA-SHA256 signature is generated correctly
- ✓ Signed payload contains all required fields
- ✓ Signature is Base64 encoded
- ✓ Certificate is in PEM format

## API Changes

### New Method

- `IQzPrintingService.ValidateSignatureConfigurationAsync()`: Validates that signing is properly configured

### Modified Behavior

- `IQzPrintingService.SubmitPrintJobAsync()`: Now signs all print payloads before sending to QZ Tray

## Dependencies

New NuGet packages added:
- `System.Security.Cryptography.Pkcs` (8.0.1)
- `System.Security.Cryptography.X509Certificates` (4.3.2)

## Compatibility

This implementation is compatible with QZ Tray's official signature requirements and follows the same pattern as demonstrated in the QZ Tray documentation and samples.