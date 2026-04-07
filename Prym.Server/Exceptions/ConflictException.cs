namespace Prym.Server.Exceptions;

/// <summary>
/// Exception thrown when an operation conflicts with existing data
/// </summary>
public class ConflictException : PrymException
{
    public ConflictException(string message) : base(message) { }
}
