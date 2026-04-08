namespace EventForge.Hardware.Interfaces;

/// <summary>
/// Result returned by a Protocol 17 (ECR17) payment terminal operation.
/// </summary>
/// <param name="Approved">True if the terminal responded with approval code "00".</param>
/// <param name="ResponseCode">2-character ASCII response code from the terminal (e.g., "00" = approved).</param>
/// <param name="AuthorizationCode">Optional 6-character authorization code returned by the terminal.</param>
/// <param name="Amount">Transaction amount in EUR echoed back by the terminal.</param>
/// <param name="ErrorMessage">Human-readable error message when Approved is false.</param>
public record Protocol17Response(
    bool Approved,
    string ResponseCode,
    string? AuthorizationCode,
    decimal Amount,
    string? ErrorMessage = null);
