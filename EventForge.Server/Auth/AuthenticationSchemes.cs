namespace EventForge.Server.Auth;

/// <summary>
/// Constants for authentication scheme names used throughout the application.
/// </summary>
public static class AuthenticationSchemes
{
    /// <summary>
    /// Cookie authentication scheme name for server-side Razor Pages authentication.
    /// </summary>
    public const string ServerCookie = "ServerCookie";

    /// <summary>
    /// Mixed authentication scheme that routes to either JWT or Cookie based on request context.
    /// </summary>
    public const string Mixed = "Mixed";
}
