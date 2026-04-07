namespace EventForge.Server.Services.FiscalPrinting.EpsonProtocol;

/// <summary>
/// Constants for the Epson POS Printer WebAPI (ePOS-Print XML) protocol.
/// Targets all Epson TM-series network printers via their embedded HTTP server.
/// Specification reference: Epson POS Printer WebAPI Interface Specification (Rev.A).
/// </summary>
public static class EpsonProtocolConstants
{
    // -------------------------------------------------------------------------
    //  HTTP / transport
    // -------------------------------------------------------------------------

    /// <summary>
    /// Default HTTP port for the Epson printer's embedded WebAPI server.
    /// Most TM-series network printers (TM-T88, TM-m30, TM-m50, etc.) expose
    /// the WebAPI on port 80.
    /// </summary>
    public const int DefaultPort = 80;

    /// <summary>
    /// REST endpoint path for all ePOS-Print XML requests.
    /// All print and management commands are POSTed to this path.
    /// </summary>
    public const string RequestEndpointPath = "/api/1/request";

    /// <summary>
    /// ePOS-Print XML namespace used in all request and response documents.
    /// </summary>
    public const string EposPrintNamespace = "http://www.epson-pos.com/schemas/2011/03/epos-print";

    /// <summary>SOAP envelope namespace.</summary>
    public const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";

    /// <summary>Content-Type header value for ePOS-Print XML requests.</summary>
    public const string ContentType = "text/xml; charset=utf-8";

    // -------------------------------------------------------------------------
    //  ePOS-Print: device / timeout defaults
    // -------------------------------------------------------------------------

    /// <summary>
    /// Default device ID sent with every request.
    /// Corresponds to the device name configured on the printer's embedded web server.
    /// Typically "local_printer" for single-printer setups; can be overridden per printer
    /// by setting <see cref="EventForge.Server.Data.Entities.Common.Printer.UsbDeviceId"/>.
    /// </summary>
    public const string DefaultDeviceId = "local_printer";

    /// <summary>Default timeout (milliseconds) sent in the ePOS-Print XML request.</summary>
    public const int DefaultTimeoutMs = 10_000;

    // -------------------------------------------------------------------------
    //  ePOS-Print: text alignment
    // -------------------------------------------------------------------------

    /// <summary>Left-aligned text.</summary>
    public const string AlignLeft = "left";

    /// <summary>Centred text.</summary>
    public const string AlignCenter = "center";

    /// <summary>Right-aligned text.</summary>
    public const string AlignRight = "right";

    // -------------------------------------------------------------------------
    //  ePOS-Print: font names
    // -------------------------------------------------------------------------

    /// <summary>Font A (12 × 24 dots, standard).</summary>
    public const string FontA = "font_a";

    /// <summary>Font B (9 × 17 dots, narrow).</summary>
    public const string FontB = "font_b";

    // -------------------------------------------------------------------------
    //  ePOS-Print: cut types
    // -------------------------------------------------------------------------

    /// <summary>Full cut with paper feed.</summary>
    public const string CutFeed = "feed";

    /// <summary>Full cut without extra paper feed.</summary>
    public const string CutNoFeed = "no_feed";

    /// <summary>Partial cut (leaves a thin strip connecting the sections).</summary>
    public const string CutReserve = "reserve";

    // -------------------------------------------------------------------------
    //  ePOS-Print: drawer pulse
    // -------------------------------------------------------------------------

    /// <summary>Cash drawer connected to drawer port 1 (pin 2).</summary>
    public const string Drawer1 = "drawer_1";

    /// <summary>Cash drawer connected to drawer port 2 (pin 5).</summary>
    public const string Drawer2 = "drawer_2";

    /// <summary>Pulse time: 100 ms.</summary>
    public const string PulseTime100 = "pulse_100";

    /// <summary>Pulse time: 200 ms.</summary>
    public const string PulseTime200 = "pulse_200";

    // -------------------------------------------------------------------------
    //  Response status bitmask
    //  Derived from the ePOS-Print API User's Manual (peripheral status word).
    // -------------------------------------------------------------------------

    /// <summary>Bit 1 – Drawer kick-out connector signal: 0 = High (drawer closed), 1 = Low (drawer open).</summary>
    public const int StatusBitDrawerOpen = 0x02;

    /// <summary>Bit 2 – Online status: 0 = offline, 1 = online.</summary>
    public const int StatusBitOnline = 0x04;

    /// <summary>Bit 5 – Paper near-end sensor: 0 = no, 1 = paper near-end.</summary>
    public const int StatusBitPaperNearEnd = 0x20;

    /// <summary>Bit 6 – Paper-end sensor: 0 = no, 1 = paper out.</summary>
    public const int StatusBitPaperOut = 0x40;

    /// <summary>Bit 9 (extended) – Cover open: 0 = closed, 1 = open.</summary>
    public const int StatusBitCoverOpen = 0x200;

    // -------------------------------------------------------------------------
    //  Response error codes
    // -------------------------------------------------------------------------

    /// <summary>No error.</summary>
    public const string CodeOk = "";

    /// <summary>Printer cover is open.</summary>
    public const string ErrCoverOpen = "EPTR_COVER_OPEN";

    /// <summary>Paper roll is empty.</summary>
    public const string ErrPaperEmpty = "EPTR_REC_EMPTY";

    /// <summary>Mechanical error (jam, head failure, etc.).</summary>
    public const string ErrMechanical = "EPTR_MECHANICAL";

    /// <summary>Auto-cutter error.</summary>
    public const string ErrAutocutter = "EPTR_AUTOCUTTER";

    /// <summary>Unrecoverable error – requires service intervention.</summary>
    public const string ErrUnrecoverable = "EPTR_UNRECOVERABLE";

    /// <summary>Printer is auto-recovering from an error.</summary>
    public const string ErrAutoRecover = "EPTR_AUTORECOVER";

    /// <summary>XML schema error – unsupported command or malformed request.</summary>
    public const string ErrSchemaError = "SchemaError";

    /// <summary>Device (printer) not found at the specified devid.</summary>
    public const string ErrDeviceNotFound = "DeviceNotFound";

    /// <summary>Print system error.</summary>
    public const string ErrPrintSystemError = "PrintSystemError";

    /// <summary>Bad port configuration.</summary>
    public const string ErrBadPort = "EX_BADPORT";

    /// <summary>Request timed out.</summary>
    public const string ErrTimeout = "EX_TIMEOUT";

    // -------------------------------------------------------------------------
    //  Receipt line formatting
    // -------------------------------------------------------------------------

    /// <summary>Maximum characters per line on 80 mm paper with Font A (42 chars).</summary>
    public const int MaxCharsPerLine80mm = 42;

    /// <summary>Maximum characters per line on 58 mm paper with Font A (32 chars).</summary>
    public const int MaxCharsPerLine58mm = 32;

    /// <summary>Default maximum characters per line used by the XML builder.</summary>
    public const int DefaultMaxCharsPerLine = MaxCharsPerLine80mm;

    // -------------------------------------------------------------------------
    //  ESC/POS raw bytes for fiscal-style operations
    // -------------------------------------------------------------------------

    /// <summary>ESC/POS: Initialize printer (ESC @).</summary>
    public static readonly byte[] EscInit = [0x1B, 0x40];

    /// <summary>ESC/POS: Feed and cut (GS V 0).</summary>
    public static readonly byte[] GsCut = [0x1D, 0x56, 0x00];

    /// <summary>ESC/POS: Open cash drawer on pin 2 (ESC p 0 ...).</summary>
    public static readonly byte[] EscOpenDrawer1 = [0x1B, 0x70, 0x00, 0x19, 0x78];
}
