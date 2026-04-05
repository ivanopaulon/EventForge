# QZ Tray Print Service Integration Guide

## Overview

EventForge integrates with QZ Tray to provide comprehensive print management capabilities for receipt and document printing. This integration enables centralized printer management, real-time status monitoring, and robust print job handling.

## QZ Tray Overview

QZ Tray is a Java-based print service that allows web applications to interface with local printers via WebSocket API. It provides:

- **Remote Network Communication**: Control printers from any network location
- **Printer Discovery**: Automatically find available printers
- **Status Monitoring**: Real-time printer status checking
- **Multiple Print Formats**: Support for raw, HTML, PDF, and image printing
- **Certificate-based Authentication**: Secure connections
- **Cross-platform Support**: Windows, macOS, and Linux

## Architecture

### Server-Side Components

1. **DTOs** (`EventForge.DTOs.Printing`)
   - `PrinterDto`: Printer information and configuration
   - `PrintJobDto`: Print job details and status
   - `QzPrintingDto`: QZ-specific request/response objects

2. **Service Interface** (`IQzPrintingService`)
   - Defines all print-related operations
   - Located in `EventForge.Server.Services.Interfaces`

3. **Service Implementation** (`QzPrintingService`)
   - WebSocket communication with QZ Tray
   - Print job management and tracking
   - Error handling and retry logic

4. **REST Controller** (`PrintingController`)
   - RESTful API endpoints
   - Authentication and authorization
   - Request validation and error handling

### Client-Side Components

1. **Client Service** (`IPrintingService`)
   - HTTP client for API communication
   - JSON serialization handling
   - Error logging and management

2. **UI Components**
   - `PrinterManagement.razor`: Printer discovery and management
   - Comprehensive UI for all print operations

## API Endpoints

### Base URL: `/api/printing`

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/discover` | Discover available printers |
| POST | `/status` | Check printer status |
| POST | `/print` | Submit print job |
| GET | `/jobs/{id}` | Get print job status |
| POST | `/jobs/{id}/cancel` | Cancel print job |
| POST | `/test-connection` | Test QZ connection |
| POST | `/version` | Get QZ version info |

### Request/Response Examples

#### Discover Printers
```json
POST /api/printing/discover
{
  "qzUrl": "ws://localhost:8182",
  "includeDetails": true,
  "checkStatus": true,
  "timeoutMs": 30000
}
```

#### Submit Print Job
```json
POST /api/printing/print
{
  "printJob": {
    "printerId": "Canon_TS3100_series",
    "title": "Receipt #12345",
    "contentType": "Raw",
    "content": "RECEIPT\n========\nTotal: $25.00\nThank you!",
    "copies": 1,
    "priority": "Normal"
  },
  "validatePrinter": true,
  "waitForCompletion": false
}
```

## QZ Tray Setup

### Installation

1. Download QZ Tray from the official website
2. Install according to your operating system:
   - **Windows**: Run the MSI installer
   - **macOS**: Install the DMG package
   - **Linux**: Use the appropriate package for your distribution

### Configuration

1. **Default Settings**
   - QZ Tray runs on `ws://localhost:8182` by default
   - No authentication required for localhost connections
   - Automatically starts with the system

2. **Network Configuration**
   - For remote access, configure firewall rules
   - Enable HTTPS for secure connections
   - Set up certificate-based authentication if needed

3. **Printer Configuration**
   - Ensure printers are installed and accessible to the system
   - Test printer functionality outside of QZ Tray first
   - Configure paper sizes and print preferences

## Usage Examples

### Basic Printer Discovery

```csharp
// Inject the service
@inject IPrintingService PrintingService

// Discover printers
var request = new PrinterDiscoveryRequestDto
{
    QzUrl = "ws://localhost:8182",
    IncludeDetails = true,
    CheckStatus = true
};

var response = await PrintingService.DiscoverPrintersAsync(request);
if (response?.Success == true)
{
    var printers = response.Printers;
    // Process discovered printers
}
```

### Print a Receipt

```csharp
var printJob = new PrintJobDto
{
    PrinterId = "thermal_printer_id",
    PrinterName = "Receipt Printer",
    Title = "Sale Receipt",
    ContentType = PrintContentType.Raw,
    Content = GenerateReceiptContent(),
    Copies = 1,
    Priority = PrintJobPriority.Normal
};

var request = new SubmitPrintJobRequestDto
{
    PrintJob = printJob,
    ValidatePrinter = true,
    WaitForCompletion = false
};

var response = await PrintingService.SubmitPrintJobAsync(request);
if (response?.Success == true)
{
    var jobId = response.PrintJob.Id;
    // Track print job status
}
```

### Monitor Print Job Status

```csharp
var printJob = await PrintingService.GetPrintJobStatusAsync(jobId);
if (printJob != null)
{
    switch (printJob.Status)
    {
        case PrintJobStatus.Completed:
            // Handle successful completion
            break;
        case PrintJobStatus.Failed:
            // Handle failure
            var error = printJob.ErrorMessage;
            break;
        case PrintJobStatus.Printing:
            // Still in progress
            break;
    }
}
```

## Best Practices

### 1. Error Handling
- Always check response success flags
- Implement retry logic for transient failures
- Log errors appropriately for debugging
- Provide user-friendly error messages

### 2. Performance Optimization
- Cache printer discovery results
- Use appropriate timeouts for operations
- Implement connection pooling if needed
- Monitor QZ Tray service health

### 3. Security Considerations
- Use HTTPS in production environments
- Implement certificate-based authentication for remote access
- Validate all print content to prevent injection attacks
- Audit print operations for compliance

### 4. User Experience
- Provide real-time status updates
- Show progress indicators for long operations
- Allow users to cancel print jobs
- Display clear error messages and recovery options

## Troubleshooting

### Common Issues

1. **QZ Tray Not Running**
   - Verify QZ Tray service is installed and running
   - Check Windows Services or system processes
   - Restart QZ Tray if necessary

2. **Connection Refused**
   - Verify the QZ URL is correct (default: `ws://localhost:8182`)
   - Check firewall settings
   - Ensure QZ Tray is listening on the expected port

3. **Printer Not Found**
   - Verify printer is installed and accessible
   - Check printer driver installation
   - Try printing from other applications first

4. **Print Job Fails**
   - Check printer status (online, paper, etc.)
   - Verify print content format is supported
   - Check for printer-specific requirements

### Debugging Tips

1. **Enable Detailed Logging**
   ```csharp
   // In appsettings.json
   {
     "Logging": {
       "LogLevel": {
         "EventForge.Server.Services.Printing": "Debug"
       }
     }
   }
   ```

2. **Test QZ Connection**
   - Use the built-in connection test functionality
   - Verify QZ Tray responds to version requests
   - Test basic printer discovery

3. **Monitor QZ Tray Logs**
   - Check QZ Tray's own log files
   - Look for WebSocket connection messages
   - Monitor for printer-related errors

## Integration with EventForge Features

### Multi-Tenant Support
- Print jobs are automatically associated with the current tenant
- Printer configurations can be tenant-specific
- Audit trails include print operations

### Real-time Updates
- Consider integrating with SignalR for real-time print status updates
- Notify users when print jobs complete or fail
- Update printer status in real-time

### User Management
- Print jobs are tracked by user
- Implement role-based printing permissions
- Audit print operations by user and tenant

## Future Enhancements

### Planned Features
1. **Print Queue Management**: Visual queue with prioritization
2. **Print Templates**: Reusable templates for common documents
3. **Batch Printing**: Multiple documents in a single operation
4. **Print Analytics**: Usage statistics and reporting
5. **Mobile Support**: Print from mobile devices via EventForge

### Configuration Enhancements
1. **Printer Profiles**: Save printer-specific settings
2. **Auto-discovery**: Periodic printer scanning
3. **Failover Support**: Backup printers for high availability
4. **Cost Tracking**: Monitor printing costs and usage

## Support and Maintenance

### Regular Maintenance
- Update QZ Tray to the latest version
- Monitor print service performance
- Clean up old print job records
- Update printer drivers as needed

### Monitoring
- Set up alerts for QZ Tray service failures
- Monitor print job success rates
- Track printer availability and status
- Log all print operations for audit purposes