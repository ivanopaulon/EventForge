using System.Text;

namespace Prym.Server.Services.FiscalPrinting.CustomProtocol;

/// <summary>
/// Enumerates the possible response types received from a Custom fiscal printer.
/// </summary>
public enum CustomResponseType
{
    /// <summary>The printer accepted the command (single ACK byte 0x06).</summary>
    Ack,

    /// <summary>The printer rejected the command (single NAK byte 0x15).</summary>
    Nak,

    /// <summary>The printer returned a data frame (STX … ETX checksum).</summary>
    Data,

    /// <summary>The response format is unrecognised or the byte array is null/empty.</summary>
    Unknown
}

/// <summary>
/// Represents the parsed result of a response received from a Custom fiscal printer.
/// </summary>
/// <param name="Type">Indicates whether the response is ACK, NAK, Data, or Unknown.</param>
/// <param name="Data">The decoded ASCII payload, populated only for <see cref="CustomResponseType.Data"/> responses.</param>
/// <param name="RawChecksum">The checksum byte as included in the response frame (last byte for Data frames).</param>
/// <param name="ComputedChecksum">The checksum byte as computed from the frame contents.</param>
/// <param name="IsChecksumValid">True when <paramref name="RawChecksum"/> matches <paramref name="ComputedChecksum"/>.</param>
public sealed record CustomResponse(
    CustomResponseType Type,
    string? Data,
    byte RawChecksum,
    byte ComputedChecksum,
    bool IsChecksumValid);

/// <summary>
/// Parses binary response frames received from a Custom fiscal printer.
/// Handles single-byte ACK/NAK responses as well as multi-byte data frames.
/// </summary>
public static class CustomResponseParser
{
    /// <summary>
    /// Parses a raw byte array received from the printer into a <see cref="CustomResponse"/>.
    /// </summary>
    /// <param name="response">Raw bytes received from the printer. Null or empty returns <see cref="CustomResponseType.Unknown"/>.</param>
    /// <returns>A <see cref="CustomResponse"/> describing the response type, payload, and checksum validity.</returns>
    public static CustomResponse ParseResponse(byte[]? response)
    {
        if (response is null || response.Length == 0)
            return new CustomResponse(CustomResponseType.Unknown, null, 0, 0, false);

        // Single-byte ACK
        if (response.Length == 1 && response[0] == CustomProtocolCommands.ACK)
            return new CustomResponse(CustomResponseType.Ack, null, 0, 0, true);

        // Single-byte NAK
        if (response.Length == 1 && response[0] == CustomProtocolCommands.NAK)
            return new CustomResponse(CustomResponseType.Nak, null, 0, 0, true);

        // Data frame: STX … ETX checksum  (minimum 4 bytes: STX + 1 data + ETX + checksum)
        if (response.Length >= 4
            && response[0] == CustomProtocolCommands.STX
            && response[^2] == CustomProtocolCommands.ETX)
        {
            byte rawChecksum = response[^1];
            byte computedChecksum = ComputeChecksum(response, 1, response.Length - 2);
            bool valid = rawChecksum == computedChecksum;

            // Extract payload: bytes between STX and ETX
            string data = Encoding.ASCII.GetString(response, 1, response.Length - 3);
            return new CustomResponse(CustomResponseType.Data, data, rawChecksum, computedChecksum, valid);
        }

        return new CustomResponse(CustomResponseType.Unknown, null, 0, 0, false);
    }

    /// <summary>
    /// Returns <c>true</c> when the response is a single ACK byte (0x06).
    /// </summary>
    /// <param name="response">Raw bytes received from the printer.</param>
    public static bool IsAck(byte[]? response)
        => response is { Length: 1 } && response[0] == CustomProtocolCommands.ACK;

    /// <summary>
    /// Returns <c>true</c> when the response is a single NAK byte (0x15).
    /// </summary>
    /// <param name="response">Raw bytes received from the printer.</param>
    public static bool IsNak(byte[]? response)
        => response is { Length: 1 } && response[0] == CustomProtocolCommands.NAK;

    /// <summary>
    /// Attempts to extract the ASCII payload from a data frame.
    /// </summary>
    /// <param name="response">Raw bytes received from the printer.</param>
    /// <param name="data">The decoded ASCII string if extraction succeeds; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if the response is a valid data frame; otherwise <c>false</c>.</returns>
    public static bool TryExtractData(byte[]? response, out string? data)
    {
        data = null;
        var parsed = ParseResponse(response);
        if (parsed.Type != CustomResponseType.Data)
            return false;

        data = parsed.Data;
        return true;
    }

    /// <summary>
    /// Verifies the XOR checksum of a data frame.
    /// Returns <c>false</c> for non-data frames (ACK, NAK, Unknown).
    /// </summary>
    /// <param name="response">Raw bytes received from the printer.</param>
    /// <returns><c>true</c> if the checksum in the frame matches the computed checksum.</returns>
    public static bool VerifyChecksum(byte[]? response)
        => ParseResponse(response).IsChecksumValid;

    // -------------------------------------------------------------------------
    //  Internal helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the XOR checksum over a range of bytes.
    /// </summary>
    /// <param name="bytes">Source byte array.</param>
    /// <param name="start">Index of the first byte to include (inclusive).</param>
    /// <param name="end">Index of the last byte to include (exclusive).</param>
    public static byte ComputeChecksum(byte[] bytes, int start, int end)
    {
        byte checksum = 0;
        for (int i = start; i < end; i++)
            checksum ^= bytes[i];
        return checksum;
    }
}
