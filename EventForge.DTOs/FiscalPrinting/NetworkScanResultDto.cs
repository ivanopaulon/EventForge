namespace EventForge.DTOs.FiscalPrinting;

/// <summary>
/// Represents a fiscal printer discovered during a network subnet scan.
/// Returned by <c>GET /api/v1/fiscal-printing/scan-network</c>.
/// </summary>
public class NetworkScanResultDto
{
    /// <summary>IP address of the discovered device.</summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>TCP port on which the fiscal printer responded (typically 9100).</summary>
    public int Port { get; set; }

    /// <summary>
    /// Suggested printer name derived from the response, if the device answered
    /// to a Custom ENQ frame. <c>null</c> when only a TCP SYN-ACK was received.
    /// </summary>
    public string? DetectedModel { get; set; }

    /// <summary>
    /// Whether the device answered to a Custom protocol ENQ frame.
    /// When <c>false</c> the port is open but the protocol is unknown.
    /// </summary>
    public bool RespondedToProtocol { get; set; }

    /// <summary>
    /// Round-trip time of the last connection attempt in milliseconds.
    /// </summary>
    public int RoundTripMs { get; set; }
}
