using System.Text;

namespace EventForge.Server.Services.PaymentTerminal.Communication;

/// <summary>
/// Builds and parses Protocol 17 (ECR17) messages for Italian POS payment terminals.
/// Protocol frame: STX | Command | Amount (10 ASCII digits, cents) | ETX | BCC
/// BCC = XOR of all bytes between STX and ETX (inclusive of ETX, exclusive of STX)
/// </summary>
internal static class Protocol17Protocol
{
    private const byte STX = 0x02;
    private const byte ETX = 0x03;

    public const string CmdPayment = "01";
    public const string CmdVoid    = "02";
    public const string CmdRefund  = "04";

    public static byte[] BuildRequest(string commandCode, decimal amountEur)
    {
        long cents = (long)Math.Round(amountEur * 100, MidpointRounding.AwayFromZero);
        var amountStr = cents.ToString("D10");

        var payload = Encoding.ASCII.GetBytes(commandCode + amountStr);

        var frame = new byte[1 + payload.Length + 1 + 1];
        frame[0] = STX;
        payload.CopyTo(frame, 1);
        frame[1 + payload.Length] = ETX;

        byte bcc = 0;
        for (int i = 1; i <= payload.Length + 1; i++)
            bcc ^= frame[i];
        frame[1 + payload.Length + 1] = bcc;

        return frame;
    }

    public static Protocol17ParsedResponse Parse(byte[] data)
    {
        if (data is null || data.Length < 4)
            return new Protocol17ParsedResponse(false, "XX", null, 0m, "Response too short");

        int stxIdx = Array.IndexOf(data, STX);
        if (stxIdx < 0)
            return new Protocol17ParsedResponse(false, "XX", null, 0m, "STX not found in response");

        int etxIdx = Array.IndexOf(data, ETX, stxIdx + 1);
        if (etxIdx < 0)
            return new Protocol17ParsedResponse(false, "XX", null, 0m, "ETX not found in response");

        if (etxIdx + 1 < data.Length)
        {
            byte expectedBcc = 0;
            for (int i = stxIdx + 1; i <= etxIdx; i++)
                expectedBcc ^= data[i];
            if (data[etxIdx + 1] != expectedBcc)
                return new Protocol17ParsedResponse(false, "XX", null, 0m, "BCC mismatch in response");
        }

        var payload = Encoding.ASCII.GetString(data, stxIdx + 1, etxIdx - stxIdx - 1);

        if (payload.Length < 2)
            return new Protocol17ParsedResponse(false, "XX", null, 0m, "Payload too short");

        var responseCode = payload[..2];
        var authCode = payload.Length >= 8 ? payload.Substring(2, 6).Trim() : null;

        decimal amount = 0m;
        if (payload.Length >= 18)
        {
            var amountStr = payload.Substring(8, 10);
            if (long.TryParse(amountStr, out var cents))
                amount = cents / 100m;
        }

        bool approved = responseCode == "00";
        string? errorMessage = approved ? null : $"Terminal declined: response code {responseCode}";

        return new Protocol17ParsedResponse(approved, responseCode,
            string.IsNullOrWhiteSpace(authCode) ? null : authCode,
            amount, errorMessage);
    }
}

internal record Protocol17ParsedResponse(
    bool Approved,
    string ResponseCode,
    string? AuthorizationCode,
    decimal Amount,
    string? ErrorMessage);
