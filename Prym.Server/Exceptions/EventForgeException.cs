namespace Prym.Server.Exceptions;

/// <summary>
/// Base exception for all Prym custom exceptions
/// </summary>
public abstract class PrymException : Exception
{
    public PrymException(string message) : base(message) { }

    public PrymException(string message, Exception innerException)
        : base(message, innerException) { }
}
