# EventForge Barcode Integration Guide

## Overview

Free Spire.Barcode for .NET has been successfully integrated into the EventForge system for server-side barcode and QR code generation.

## Features

### Supported Barcode Types
- **QR Code** - Universal 2D barcode for URLs, contact info, etc.
- **Code 128** - High-density linear barcode for ASCII data
- **Code 39** - Simple alphanumeric barcode
- **Code 39 Extended** - Extended ASCII support
- **Code 93** - Compact alternative to Code 39
- **Code 93 Extended** - Extended ASCII support for Code 93
- **EAN-8** - 8-digit European Article Number
- **EAN-13** - 13-digit European Article Number (most common retail)
- **EAN-128** - Variable length ASCII barcode
- **EAN-14** - 14-digit shipping container code
- **Code 11** - Numeric with dash support
- **Codabar** - Legacy format for libraries and blood banks
- **Code 25** - Numeric only (Standard 2 of 5)
- **Interleaved 25** - Numeric pairs (Interleaved 2 of 5)

### Supported Image Formats
- PNG (recommended)
- JPEG
- BMP
- GIF

## API Endpoints

All endpoints are under `/api/barcode` and require authentication.

### 1. Generate Barcode/QR Code
**POST** `/api/barcode/generate`

**Request Body:**
```json
{
  "data": "https://eventforge.com",
  "barcodeType": "QRCode",
  "width": 300,
  "height": 300,
  "imageFormat": "PNG"
}
```

**Response:**
```json
{
  "base64Image": "iVBORw0KGgoAAAANSUhEUgAAA...",
  "mimeType": "image/png",
  "width": 300,
  "height": 300,
  "barcodeType": "QRCode",
  "data": "https://eventforge.com"
}
```

### 2. Generate QR Code (Quick)
**POST** `/api/barcode/qr`

**Request Body:** (raw string)
```
"https://eventforge.com"
```

### 3. Validate Data
**POST** `/api/barcode/validate?data=123456789&barcodeType=Code128`

**Response:**
```json
true
```

### 4. Get Supported Types
**GET** `/api/barcode/types`

**Response:**
```json
[
  "QRCode",
  "Code128",
  "Code39",
  // ... all supported types
]
```

### 5. Get Supported Formats
**GET** `/api/barcode/formats`

**Response:**
```json
[
  "PNG",
  "JPEG",
  "BMP",
  "GIF"
]
```

## Usage Examples

### Event Ticket QR Code
```json
{
  "data": "TICKET:EVENT123:SEAT456:USER789",
  "barcodeType": "QRCode",
  "width": 200,
  "height": 200,
  "imageFormat": "PNG"
}
```

### Product Barcode
```json
{
  "data": "1234567890128",
  "barcodeType": "EAN13",
  "width": 200,
  "height": 80,
  "imageFormat": "PNG"
}
```

### Simple Text QR Code
```json
{
  "data": "Contact info or any text",
  "barcodeType": "QRCode",
  "width": 150,
  "height": 150,
  "imageFormat": "PNG"
}
```

## Implementation Details

### Service Architecture
- **IBarcodeService**: Interface defining barcode operations
- **BarcodeService**: Implementation using Free Spire.Barcode
- **BarcodeController**: REST API endpoints
- **DTOs**: Request/Response data transfer objects

### Dependency Injection
The service is registered as scoped in the DI container:
```csharp
services.AddScoped<IBarcodeService, BarcodeService>();
```

### Validation
The service includes comprehensive validation for each barcode type:
- QR codes: up to 4,296 characters
- Code 128: ASCII characters only
- EAN-13: exactly 13 digits
- Code 39: specific character set only
- etc.

## Error Handling

The API includes proper error handling:
- **400 Bad Request**: Invalid data format for barcode type
- **500 Internal Server Error**: Generation failures
- **401 Unauthorized**: Missing or invalid authentication

## Security

- All endpoints require authentication
- Input validation prevents malicious data
- Reasonable size limits on generated images

## Best Practices

1. **Use QR codes** for complex data like URLs, JSON, or multi-line text
2. **Use EAN-13/EAN-8** for retail products
3. **Use Code 128** for inventory tracking with mixed alphanumeric data
4. **Validate data** before generation to ensure compatibility
5. **Choose appropriate dimensions** based on scanning distance and density

## Platform Compatibility

- **Windows**: Full functionality including image generation
- **Linux/macOS**: Validation and API structure work, but image generation may require additional GDI+ libraries

## Future Enhancements

Potential improvements could include:
- Batch generation endpoints
- Custom color schemes
- Error correction level settings for QR codes
- Integration with event and product entities
- Caching for frequently generated codes