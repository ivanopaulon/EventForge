namespace Prym.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// Exception thrown when a communication error occurs with a Custom fiscal printer.
/// Wraps transport-specific exceptions (socket errors, serial I/O errors) in a
/// protocol-neutral type so callers do not need to handle transport details.
/// </summary>
public sealed class FiscalPrinterCommunicationException : Exception
{
    /// <summary>Initializes a new instance with a descriptive message.</summary>
    public FiscalPrinterCommunicationException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    public FiscalPrinterCommunicationException(string message, Exception innerException)
        : base(message, innerException) { }
}
