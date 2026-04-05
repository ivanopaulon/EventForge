# QZ Tray Enhanced Digital Signature Implementation

This document describes the enhanced implementation of digital signature functionality for QZ Tray print requests in EventForge, now supporting complete certificate chains, timestamps, UIDs, and the full QZ Tray demo payload structure.

## Overview

The QZ Tray printing service now supports comprehensive RSA-SHA256 digital signatures for all print requests with the following enhancements:
- **Complete certificate chain** with intermediate certificate support and proper concatenation markers
- **UTC timestamp** in milliseconds for request tracking
- **Short UID** generation (base64 GUID format similar to QZ Tray demo)
- **Position field** as required by QZ Tray demo structure
- **Proper signature calculation** on the complete payload (excluding signature field itself)

## Enhanced Implementation Details

### Components

1. **QzDigitalSignatureService**: Enhanced to handle complete certificate chains, timestamps, UIDs, and proper payload structure
2. **QzPrintingService**: Updated to use the enhanced signature service with all required fields
3. **PrintingController**: Added test endpoint for signature validation
4. **Certificate Files**: Support for both leaf and intermediate certificates with proper concatenation

### Files Modified/Enhanced

- `EventForge.Server/Services/Printing/QzDigitalSignatureService.cs` - **ENHANCED** with certificate chain, timestamp, UID, and position support
- `EventForge.Server/Controllers/PrintingController.cs` - **ENHANCED** with test endpoint for signature validation
- `EventForge.Server/appsettings.json` - **ENHANCED** with intermediate certificate configuration
- `EventForge.Server/intermediate-certificate.txt` - **NEW** intermediate certificate for chain testing

### Enhanced Configuration

```json
{
  "QzSigning": {
    "PrivateKeyPath": "private-key.pem",
    "CertificatePath": "digital-certificate.txt",
    "IntermediateCertificatePath": "intermediate-certificate.txt"
  }
}
```

### Enhanced Payload Structure

The enhanced signed payload now matches the QZ Tray demo structure exactly:

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
  "certificate": "-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----\n--START INTERMEDIATE CERT--\n-----BEGIN CERTIFICATE-----\n...\n-----END CERTIFICATE-----",
  "timestamp": 1754693911671,
  "uid": "j5lxuw",
  "signature": "<Base64-encoded RSA-SHA256 signature>",
  "position": { "x": 960, "y": 516 }
}
```

## Key Enhancements

### 1. Complete Certificate Chain Support

- **Leaf Certificate**: Primary certificate loaded from `digital-certificate.txt`
- **Intermediate Certificate**: Optional intermediate certificate from `intermediate-certificate.txt`
- **Chain Concatenation**: Proper concatenation with `--START INTERMEDIATE CERT--` markers as required by QZ Tray
- **Automatic Detection**: Service automatically detects and includes intermediate certificates when configured

### 2. Enhanced Timestamp Generation

- **UTC Milliseconds**: Timestamp generated as UTC milliseconds (`DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`)
- **Request-Time Generation**: Timestamp generated at the exact moment of signature creation
- **Compatibility**: Format matches QZ Tray demo requirements

### 3. Short UID Generation

- **GUID-Based**: Generated from new GUID for uniqueness
- **Base64 Encoding**: Converted to base64 and cleaned (no padding, no special chars)
- **Short Format**: 6-8 characters similar to QZ Tray demo format
- **Lowercase**: Normalized to lowercase for consistency

### 4. Complete Payload Structure

- **Position Field**: Added default position `{x: 960, y: 516}` as per QZ Tray demo
- **Field Order**: Maintains proper field order for signature calculation
- **Signature Calculation**: RSA-SHA256 signature calculated on complete payload BEFORE adding signature field

### 5. Enhanced Validation

- **Configuration Validation**: Checks for both leaf and intermediate certificates
- **Certificate Chain Validation**: Validates complete certificate chain loading
- **Test Endpoint**: New `/api/printing/test-signature` endpoint for validation

## Testing

### Enhanced Test Endpoint

A new test endpoint is available for validating the enhanced signature functionality:

```
POST /api/printing/test-signature
```

This endpoint:
- ✅ Validates signature configuration with certificate chain
- ✅ Creates a test print payload with all required fields
- ✅ Signs the payload using the enhanced service
- ✅ Returns detailed test results and payload structure
- ✅ Confirms all QZ Tray requirements are met

### Validation Checklist

The enhanced implementation validates:
- ✅ Complete certificate chain with intermediate markers
- ✅ UTC timestamp in milliseconds
- ✅ Short base64 UID generation (6-8 chars, lowercase)
- ✅ RSA-SHA256 signature on complete payload
- ✅ Position field as per QZ Tray demo
- ✅ Payload structure matches QZ Tray requirements exactly

## Security Enhancements

### Certificate Chain Management

- **Complete Chain Validation**: Validates both leaf and intermediate certificates
- **Proper Concatenation**: Uses QZ Tray required markers for intermediate certificates
- **Chain Integrity**: Ensures complete certificate chain is included in every signed request

### Enhanced Signature Security

- **Complete Payload Signing**: Signature calculated on the complete payload including timestamp and UID
- **Proper Field Order**: Ensures signature is calculated AFTER all other fields are set
- **Timestamp Integrity**: Each request has a unique timestamp preventing replay attacks

## Compatibility

### QZ Tray Compatibility

- ✅ **Certificate Chain**: Supports QZ Tray's complete certificate chain requirements
- ✅ **Intermediate Certificates**: Proper concatenation with `--START INTERMEDIATE CERT--` markers
- ✅ **Payload Structure**: Exactly matches QZ Tray demo payload structure
- ✅ **Signature Calculation**: Compatible with QZ Tray signature verification
- ✅ **Field Requirements**: All required fields (call, params, certificate, timestamp, uid, signature, position)

### Demo Compatibility

The enhanced implementation produces payloads that are fully compatible with:
- QZ Tray official demo structure
- QZ Tray certificate chain requirements
- QZ Tray signature verification process
- QZ Tray timestamp and UID expectations

## API Enhancements

### Enhanced Methods

- `QzDigitalSignatureService.SignPayloadAsync()`: **ENHANCED** with complete certificate chain, timestamp, UID, and position support
- `QzDigitalSignatureService.LoadCertificateChainAsync()`: **NEW** method for loading complete certificate chains
- `QzDigitalSignatureService.GenerateUid()`: **NEW** method for QZ Tray compatible UID generation
- `PrintingController.TestEnhancedSignature()`: **NEW** endpoint for comprehensive signature testing

### Enhanced Response Structure

All print requests now return enhanced signed payloads with:
- Complete certificate chain
- UTC timestamp in milliseconds
- Short UID (6-8 characters)
- RSA-SHA256 signature
- Position field
- Full QZ Tray compatibility

## Upgrade Notes

### Breaking Changes

- **None**: The enhanced implementation is fully backward compatible

### New Features

- Certificate chain support with intermediate certificates
- Enhanced payload structure with timestamp, UID, and position
- Test endpoint for signature validation
- Complete QZ Tray demo compatibility

### Configuration Updates

Update `appsettings.json` to include intermediate certificate path (optional):

```json
{
  "QzSigning": {
    "PrivateKeyPath": "private-key.pem",
    "CertificatePath": "digital-certificate.txt",
    "IntermediateCertificatePath": "intermediate-certificate.txt"
  }
}
```

## Benefits

### Enhanced Security
- Complete certificate chain validation
- Unique timestamps prevent replay attacks
- Comprehensive payload signing

### QZ Tray Compatibility
- 100% compatible with QZ Tray requirements
- Matches official demo structure exactly
- Supports all QZ Tray signature verification features

### Better Debugging
- Test endpoint for easy validation
- Enhanced logging for troubleshooting
- Clear error messages for configuration issues

### Future-Proof
- Extensible certificate chain support
- Modular UID generation
- Flexible payload structure

This enhanced implementation ensures that EventForge's QZ Tray integration meets all current and future QZ Tray digital signature requirements while maintaining full compatibility with the official QZ Tray demo structure.