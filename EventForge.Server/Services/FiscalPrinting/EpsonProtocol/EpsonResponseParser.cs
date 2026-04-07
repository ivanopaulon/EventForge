using System.Xml.Linq;
using EventForge.DTOs.FiscalPrinting;

namespace EventForge.Server.Services.FiscalPrinting.EpsonProtocol;

/// <summary>
/// Represents the parsed result of an Epson POS Printer WebAPI response.
/// </summary>
public sealed class EpsonResponse
{
    /// <summary>Whether the printer accepted and processed the request successfully.</summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error code returned by the printer.
    /// Empty string on success. See <see cref="EpsonProtocolConstants"/> for known codes.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Printer peripheral status bitmask returned in the <c>status</c> attribute.
    /// Parse with <see cref="EpsonResponseParser.ParseStatusBits"/>.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Human-readable error description derived from <see cref="Code"/>.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Parses XML responses returned by the Epson POS Printer WebAPI
/// (<c>POST /api/1/request</c>).
/// </summary>
public static class EpsonResponseParser
{
    // -------------------------------------------------------------------------
    //  Response parsing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses the raw XML string returned by the printer's WebAPI endpoint.
    /// </summary>
    /// <param name="xmlResponse">Raw XML (SOAP envelope) response body.</param>
    /// <returns>A populated <see cref="EpsonResponse"/> instance.</returns>
    public static EpsonResponse ParseResponse(string xmlResponse)
    {
        if (string.IsNullOrWhiteSpace(xmlResponse))
        {
            return new EpsonResponse
            {
                Success = false,
                ErrorMessage = "Empty response received from printer"
            };
        }

        try
        {
            var doc = XDocument.Parse(xmlResponse);

            // Navigate through the SOAP Body to the <response> element.
            // The response element belongs to the ePOS-Print namespace.
            var responseElement = doc.Descendants()
                .FirstOrDefault(e =>
                    e.Name.LocalName == "response"
                    && (string.IsNullOrEmpty(e.Name.NamespaceName)
                        || e.Name.NamespaceName.Contains("epson-pos", StringComparison.OrdinalIgnoreCase)));

            if (responseElement is null)
            {
                return new EpsonResponse
                {
                    Success = false,
                    ErrorMessage = $"Unexpected response structure: no <response> element found. Raw: {Truncate(xmlResponse, 300)}"
                };
            }

            bool success = string.Equals(
                responseElement.Attribute("success")?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase);

            string code = responseElement.Attribute("code")?.Value ?? string.Empty;
            string status = responseElement.Attribute("status")?.Value ?? string.Empty;

            string? errorMessage = null;
            if (!success)
                errorMessage = BuildErrorMessage(code, status);

            return new EpsonResponse
            {
                Success = success,
                Code = code,
                Status = status,
                ErrorMessage = errorMessage
            };
        }
        catch (Exception ex)
        {
            return new EpsonResponse
            {
                Success = false,
                ErrorMessage = $"Failed to parse printer response: {ex.Message}. Raw: {Truncate(xmlResponse, 300)}"
            };
        }
    }

    // -------------------------------------------------------------------------
    //  Status bitmask parsing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Parses the <c>status</c> attribute from the printer response into a
    /// <see cref="FiscalPrinterStatus"/> object.
    /// </summary>
    /// <param name="xmlResponse">Raw XML (SOAP) response from the printer.</param>
    /// <returns>
    /// A <see cref="FiscalPrinterStatus"/> reflecting the current printer state.
    /// <see cref="FiscalPrinterStatus.IsOnline"/> is <c>false</c> on error or parse failure.
    /// </returns>
    public static FiscalPrinterStatus ParseStatusResponse(string xmlResponse)
    {
        var baseResponse = ParseResponse(xmlResponse);

        if (!baseResponse.Success)
        {
            return new FiscalPrinterStatus
            {
                IsOnline = false,
                LastCheck = DateTime.UtcNow,
                LastError = baseResponse.ErrorMessage ?? "Printer returned an error"
            };
        }

        var status = new FiscalPrinterStatus
        {
            IsOnline = true,
            LastCheck = DateTime.UtcNow
        };

        ParseStatusBits(baseResponse.Status, status);

        status.PaperStatus = status.IsPaperOut ? "OUT"
            : status.IsPaperLow ? "LOW"
            : "OK";

        return status;
    }

    /// <summary>
    /// Decodes the status bitmask from the <c>status</c> attribute (decimal integer string)
    /// into the corresponding flags of <paramref name="target"/>.
    /// </summary>
    /// <param name="statusString">
    /// Decimal or hexadecimal integer string from the <c>status</c> attribute of the
    /// ePOS-Print response.
    /// </param>
    /// <param name="target">Status object to populate.</param>
    public static void ParseStatusBits(string? statusString, FiscalPrinterStatus target)
    {
        if (string.IsNullOrEmpty(statusString)) return;

        // Try decimal first, then hexadecimal
        if (!int.TryParse(statusString, out int bits))
        {
            int.TryParse(statusString,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out bits);
        }

        // ePOS-Print status bits (from User's Manual §4.2.7)
        target.IsDrawerOpen = (bits & EpsonProtocolConstants.StatusBitDrawerOpen) != 0;
        target.IsOnline = (bits & EpsonProtocolConstants.StatusBitOnline) != 0;
        target.IsPaperLow = (bits & EpsonProtocolConstants.StatusBitPaperNearEnd) != 0;
        target.IsPaperOut = (bits & EpsonProtocolConstants.StatusBitPaperOut) != 0;
        target.IsCoverOpen = (bits & EpsonProtocolConstants.StatusBitCoverOpen) != 0;

        // Standard Epson receipt printers don't have fiscal memory
        target.IsFiscalMemoryFull = false;
        target.IsFiscalMemoryAlmostFull = false;
        target.IsFiscalModeActive = false;
        target.IsDailyClosureRequired = false;
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private static string BuildErrorMessage(string code, string status)
    {
        var description = code switch
        {
            EpsonProtocolConstants.ErrCoverOpen => "Printer cover is open",
            EpsonProtocolConstants.ErrPaperEmpty => "Paper roll is empty – please replace the roll",
            EpsonProtocolConstants.ErrMechanical => "Mechanical error – printer requires service",
            EpsonProtocolConstants.ErrAutocutter => "Auto-cutter error",
            EpsonProtocolConstants.ErrUnrecoverable => "Unrecoverable error – printer requires service",
            EpsonProtocolConstants.ErrAutoRecover => "Printer is recovering from an error",
            EpsonProtocolConstants.ErrSchemaError => "Invalid XML: unsupported command or malformed request",
            EpsonProtocolConstants.ErrDeviceNotFound => "Printer device not found – check the device ID (devid)",
            EpsonProtocolConstants.ErrPrintSystemError => "Print system error",
            EpsonProtocolConstants.ErrBadPort => "Bad port configuration",
            EpsonProtocolConstants.ErrTimeout => "Request timed out",
            _ when string.IsNullOrEmpty(code) => null,
            _ => $"Printer error: {code}"
        };

        if (description is not null && !string.IsNullOrEmpty(status))
            return $"{description} (status: {status})";

        return description
            ?? (!string.IsNullOrEmpty(status) ? $"Printer error (status: {status})" : "Unknown printer error");
    }

    private static string Truncate(string text, int maxLength)
        => text.Length > maxLength ? text[..maxLength] + "..." : text;
}
