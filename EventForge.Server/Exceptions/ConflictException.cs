namespace EventForge.Server.Exceptions;

/// <summary>
/// Exception thrown when an operation conflicts with existing data
/// </summary>
public class ConflictException : EventForgeException
{
    public ConflictException(string message) : base(message) { }
}
