namespace EventForge.DTOs.Setup;

/// <summary>
/// SQL authentication credentials.
/// </summary>
public class SqlCredentials
{
    /// <summary>
    /// Authentication type (Windows or SQL).
    /// </summary>
    public string AuthenticationType { get; set; } = "Windows"; // Windows, SQL

    /// <summary>
    /// SQL Server username (for SQL auth).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SQL Server password (for SQL auth).
    /// </summary>
    public string? Password { get; set; }
}
