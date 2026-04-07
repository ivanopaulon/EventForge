namespace Prym.Server.Exceptions;

/// <summary>
/// Exception thrown when user is authenticated but lacks permission
/// </summary>
public class ForbiddenException : PrymException
{
    public ForbiddenException(string message) : base(message) { }
}
