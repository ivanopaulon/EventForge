namespace EventForge.Server.Exceptions;

/// <summary>
/// Exception thrown when user is authenticated but lacks permission
/// </summary>
public class ForbiddenException : EventForgeException
{
    public ForbiddenException(string message) : base(message) { }
}
