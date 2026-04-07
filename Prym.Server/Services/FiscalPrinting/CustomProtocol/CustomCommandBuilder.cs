using System.Text;

namespace Prym.Server.Services.FiscalPrinting.CustomProtocol;

/// <summary>
/// Builder for constructing binary command frames for the Custom fiscal printer protocol.
/// Frames follow the structure: <c>STX | commandCode | [FS field]* | ETX | checksum</c>
/// where the checksum is the XOR of all bytes between STX and ETX (exclusive).
/// </summary>
/// <remarks>
/// Usage example:
/// <code>
/// byte[] command = new CustomCommandBuilder()
///     .StartCommand(CustomProtocolCommands.CMD_PRINT_ITEM)
///     .AddField("Caffè espresso")
///     .AddField(1)
///     .AddField(1.50m)
///     .AddField(1)
///     .AddField(CustomProtocolCommands.DEPT_DEFAULT)
///     .Build();
/// </code>
/// </remarks>
public sealed class CustomCommandBuilder
{
    private readonly List<byte> _buffer = [];
    private bool _started;

    // -------------------------------------------------------------------------
    //  Builder methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Starts a new command frame by writing STX followed by the ASCII-encoded command code.
    /// Must be called before any AddField call.
    /// </summary>
    /// <param name="commandCode">Two-character command code (e.g., "01", "02"). Must not be null or whitespace.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="commandCode"/> is null or whitespace.</exception>
    public CustomCommandBuilder StartCommand(string commandCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandCode);

        _buffer.Clear();
        _buffer.Add(CustomProtocolCommands.STX);
        _buffer.AddRange(Encoding.ASCII.GetBytes(commandCode));
        _started = true;
        return this;
    }

    /// <summary>
    /// Appends a string field to the command frame, preceded by a Field Separator (FS) byte.
    /// A null or empty value writes only the FS byte (empty field).
    /// </summary>
    /// <param name="value">Field value to append.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="StartCommand"/> has not been called.</exception>
    public CustomCommandBuilder AddField(string? value)
    {
        EnsureStarted();
        _buffer.Add(CustomProtocolCommands.FS);
        if (!string.IsNullOrEmpty(value))
            _buffer.AddRange(Encoding.ASCII.GetBytes(value));
        return this;
    }

    /// <summary>
    /// Appends an integer field to the command frame, preceded by a Field Separator (FS) byte.
    /// </summary>
    /// <param name="value">Integer value to append.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="StartCommand"/> has not been called.</exception>
    public CustomCommandBuilder AddField(int value)
        => AddField(value.ToString());

    /// <summary>
    /// Appends a decimal field to the command frame, preceded by a Field Separator (FS) byte.
    /// The value is formatted as a fixed-point integer string without the decimal separator
    /// (e.g., 12.50 with 2 decimals → "1250", 1.500 with 3 decimals → "1500").
    /// </summary>
    /// <param name="value">Decimal value to append.</param>
    /// <param name="decimals">Number of decimal places to encode (default: 2).</param>
    /// <returns>The current builder instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="StartCommand"/> has not been called.</exception>
    public CustomCommandBuilder AddField(decimal value, int decimals = 2)
    {
        var multiplier = (decimal)Math.Pow(10, decimals);
        var encoded = ((long)Math.Round(value * multiplier, MidpointRounding.AwayFromZero)).ToString();
        return AddField(encoded);
    }

    /// <summary>
    /// Finalises the command frame by appending ETX and the XOR checksum byte,
    /// then returns the complete byte array ready for transmission.
    /// </summary>
    /// <returns>The complete command frame as a byte array.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="StartCommand"/> has not been called.</exception>
    public byte[] Build()
    {
        EnsureStarted();

        _buffer.Add(CustomProtocolCommands.ETX);

        // XOR checksum over all bytes between STX and ETX (exclusive):
        // bytes[1] through bytes[^2] (the ETX we just added is at [^1])
        byte checksum = 0;
        for (int i = 1; i < _buffer.Count - 1; i++)
            checksum ^= _buffer[i];

        _buffer.Add(checksum);

        return [.. _buffer];
    }

    // -------------------------------------------------------------------------
    //  Static helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a single-byte ENQ status-request frame used to query the printer's current status.
    /// The printer responds with a 3-byte bitmap that can be parsed by <see cref="CustomStatusParser"/>.
    /// </summary>
    /// <returns>A single-byte array containing the ENQ byte.</returns>
    public static byte[] StatusRequest()
        => [CustomProtocolCommands.ENQ];

    // -------------------------------------------------------------------------
    //  Debug helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns a hex-encoded string representation of the bytes accumulated so far,
    /// useful for debug logging (e.g., "02 30 31 1C 03 0A").
    /// Note: includes the ETX and checksum only if <see cref="Build"/> has been called.
    /// </summary>
    /// <returns>Space-separated uppercase hex string.</returns>
    public string ToHexString()
        => string.Join(" ", _buffer.Select(b => b.ToString("X2")));

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private void EnsureStarted()
    {
        if (!_started)
            throw new InvalidOperationException("StartCommand() must be called before adding fields or building.");
    }
}
