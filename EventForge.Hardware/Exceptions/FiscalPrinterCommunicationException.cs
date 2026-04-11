namespace Prym.Hardware.Exceptions;

/// <summary>
/// Thrown when a low-level communication error occurs while sending commands to or
/// reading responses from a fiscal printer (TCP, serial, USB, or agent-proxy channel).
/// </summary>
public class FiscalPrinterCommunicationException : Exception
{
    /// <inheritdoc />
    public FiscalPrinterCommunicationException() { }

    /// <inheritdoc />
    public FiscalPrinterCommunicationException(string message)
        : base(message) { }

    /// <inheritdoc />
    public FiscalPrinterCommunicationException(string message, Exception innerException)
        : base(message, innerException) { }
}
