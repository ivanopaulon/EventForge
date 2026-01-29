namespace EventForge.Server.Exceptions;

/// <summary>
/// Base exception for all EventForge custom exceptions
/// </summary>
public abstract class EventForgeException : Exception
{
    public EventForgeException(string message) : base(message) { }
    
    public EventForgeException(string message, Exception innerException) 
        : base(message, innerException) { }
}
