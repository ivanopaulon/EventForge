using EventForge.Server.Services.Interfaces;

namespace EventForge.Server.Services.Events;

/// <summary>
/// Example extension showing how to integrate barcode generation with existing services
/// This is a demonstration of best practices for extending EventService with barcode functionality
/// </summary>
public class EventBarcodeExtensions(
    IBarcodeService barcodeService,
    ILogger<EventBarcodeExtensions> logger)
{

    /// <summary>
    /// Generates a QR code for an event containing event details
    /// </summary>
    /// <param name="eventDto">The event to generate QR code for</param>
    /// <param name="includeTicketUrl">Whether to include a ticket purchase URL</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>QR code as base64 image</returns>
    public async Task<BarcodeResponseDto> GenerateEventQRCodeAsync(EventDto eventDto, bool includeTicketUrl = true, CancellationToken ct = default)
    {
        try
        {
            // Create event data for QR code
            var eventData = $"EVENT:{eventDto.Id}|{eventDto.Name}";

            if (includeTicketUrl)
            {
                eventData += $"|URL:https://eventforge.com/events/{eventDto.Id}";
            }

            eventData += $"|DATE:{eventDto.StartDate:yyyy-MM-dd}";

            if (!string.IsNullOrEmpty(eventDto.Location))
            {
                eventData += $"|LOC:{eventDto.Location}";
            }

            var request = new BarcodeRequestDto
            {
                Data = eventData,
                BarcodeType = BarcodeType.QRCode,
                Width = 300,
                Height = 300,
                ImageFormat = ImageFormat.PNG
            };

            return await barcodeService.GenerateBarcodeAsync(request, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Generates a ticket barcode for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="ticketId">Unique ticket ID</param>
    /// <param name="userId">User ID who purchased the ticket</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Barcode as base64 image suitable for ticket printing</returns>
    public async Task<BarcodeResponseDto> GenerateTicketBarcodeAsync(Guid eventId, string ticketId, int userId, CancellationToken ct = default)
    {
        try
        {
            // Create secure ticket data
            var ticketData = $"TKT:{eventId}:{ticketId}:{userId}:{DateTime.UtcNow:yyyyMMdd}";

            var request = new BarcodeRequestDto
            {
                Data = ticketData,
                BarcodeType = BarcodeType.Code128,
                Width = 400,
                Height = 100,
                ImageFormat = ImageFormat.PNG
            };

            return await barcodeService.GenerateBarcodeAsync(request, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Generates a simple event ID barcode for inventory/tracking purposes
    /// </summary>
    /// <param name="eventId">Event ID to encode</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Simple barcode for event tracking</returns>
    public async Task<BarcodeResponseDto> GenerateEventTrackingBarcodeAsync(Guid eventId, CancellationToken ct = default)
    {
        try
        {
            // Simple event ID encoding - using first 8 characters of GUID
            var eventIdStr = eventId.ToString("N")[..8].ToUpperInvariant();

            var request = new BarcodeRequestDto
            {
                Data = eventIdStr,
                BarcodeType = BarcodeType.Code39,
                Width = 200,
                Height = 80,
                ImageFormat = ImageFormat.PNG
            };

            return await barcodeService.GenerateBarcodeAsync(request, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Validates if event data can be encoded in the specified barcode type
    /// </summary>
    /// <param name="eventData">Event data to validate</param>
    /// <param name="barcodeType">Target barcode type</param>
    /// <returns>True if data is valid for the barcode type</returns>
    public bool ValidateEventDataForBarcode(string eventData, BarcodeType barcodeType)
    {
        return barcodeService.ValidateDataForBarcodeType(eventData, barcodeType);
    }

}

/// <summary>
/// DTO for event barcode generation requests
/// </summary>
public class EventBarcodeRequestDto
{
    public Guid EventId { get; set; }
    public BarcodeType BarcodeType { get; set; } = BarcodeType.QRCode;
    public bool IncludeTicketUrl { get; set; } = true;
    public string? TicketId { get; set; }
    public int? UserId { get; set; }
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 300;
    public ImageFormat ImageFormat { get; set; } = ImageFormat.PNG;
}
