using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.FiscalPrinting;

/// <summary>
/// Payload sent by the wizard on Step 7 ("Save Configuration") to create
/// a new fiscal printer record and persist all mappings in a single request.
/// </summary>
public class FiscalPrinterSetupDto
{
    // ── Step 1 / 2: Connection ────────────────────────────────────────────────

    /// <summary>Connection type: <c>"TCP"</c> or <c>"Serial"</c>.</summary>
    [Required]
    [MaxLength(20)]
    public string ConnectionType { get; set; } = "TCP";

    /// <summary>IP address for TCP connection (e.g., "192.168.1.100"). Required when ConnectionType = TCP.</summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>TCP port number (default 9100). Required when ConnectionType = TCP.</summary>
    [Range(1, 65535)]
    public int? TcpPort { get; set; }

    /// <summary>Serial port name (e.g., "COM1", "/dev/ttyUSB0"). Required when ConnectionType = Serial.</summary>
    [MaxLength(20)]
    public string? SerialPortName { get; set; }

    /// <summary>Serial baud rate (e.g., 9600, 115200). Required when ConnectionType = Serial.</summary>
    [Range(300, 115200)]
    public int? BaudRate { get; set; }

    // ── Step 4: Base Configuration ────────────────────────────────────────────

    /// <summary>Display name for the printer (e.g., "Stampante Fiscale Cassa 1").</summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Physical location description (e.g., "Cassa principale, piano terra").</summary>
    [MaxLength(50)]
    public string? Location { get; set; }

    /// <summary>Protocol type. Currently only "Custom" is supported.</summary>
    [MaxLength(50)]
    public string ProtocolType { get; set; } = "Custom";

    // ── Step 5: Fiscal Code Mapping ───────────────────────────────────────────

    /// <summary>
    /// VAT fiscal code overrides: maps VatRateId → fiscal code (1-10).
    /// Only entries that differ from the current DB value need to be included.
    /// </summary>
    public Dictionary<Guid, int> VatFiscalCodeOverrides { get; set; } = new();

    /// <summary>
    /// Payment method fiscal code overrides: maps PaymentMethodId → fiscal code (1-10).
    /// </summary>
    public Dictionary<Guid, int> PaymentFiscalCodeOverrides { get; set; } = new();

    // ── Step 6: POS Association ───────────────────────────────────────────────

    /// <summary>
    /// Station (POS) IDs to which this printer should be associated as default fiscal printer.
    /// </summary>
    public List<Guid> AssociatedStationIds { get; set; } = new();
}
