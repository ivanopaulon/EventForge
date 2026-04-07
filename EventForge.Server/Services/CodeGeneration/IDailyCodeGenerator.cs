namespace EventForge.Server.Services.CodeGeneration;

/// <summary>
/// Interface for generating unique daily sequential codes.
/// </summary>
public interface IDailyCodeGenerator
{
    /// <summary>
    /// Generates a unique code in the format YYYYMMDDNNNNNN (UTC date + 6-digit counter).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A unique code string.</returns>
    Task<string> GenerateDailyCodeAsync(CancellationToken cancellationToken = default);
}
