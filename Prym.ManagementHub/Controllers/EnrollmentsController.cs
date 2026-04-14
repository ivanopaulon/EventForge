using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Prym.ManagementHub.Controllers;

/// <summary>
/// Agent self-enrollment endpoint.
/// Allows a new Agent to request its own API key without admin intervention,
/// provided it presents the correct EnrollmentToken configured on the Hub.
/// </summary>
[ApiController]
[Route("api/v1/enrollments")]
public class EnrollmentsController(
    IInstallationService installationService,
    IAdminAuthService adminAuth,
    ManagementHubOptions hubOptions,
    ILogger<EnrollmentsController> logger) : ControllerBase
{
    /// <summary>
    /// Request a new API key for a fresh Agent installation.
    /// The Agent must supply the shared EnrollmentToken set on the Hub
    /// and a unique InstallationCode generated on first startup.
    /// On success the Hub creates an Installation record and returns the ApiKey
    /// together with the confirmed InstallationId.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("enrollment")]
    public async Task<IActionResult> Enroll([FromBody] EnrollmentRequest request)
    {
        if (!hubOptions.AllowAutoEnrollment || string.IsNullOrWhiteSpace(hubOptions.EnrollmentToken))
        {
            logger.LogWarning("Enrollment attempt rejected — auto-enrollment is disabled. Code={Code}", request.InstallationCode);
            return StatusCode(StatusCodes.Status403Forbidden,
                "Auto-enrollment is disabled on this Hub. Ask the administrator to register the installation manually.");
        }

        // Timing-safe comparison to prevent secret enumeration via response timing.
        if (!TokenEquals(request.EnrollmentToken, hubOptions.EnrollmentToken))
        {
            logger.LogWarning("Enrollment attempt with invalid token. Code={Code}", request.InstallationCode);
            return Unauthorized("Invalid enrollment token.");
        }

        if (string.IsNullOrWhiteSpace(request.InstallationCode))
            return BadRequest("InstallationCode is required.");

        // ── Check if this InstallationCode was already enrolled ──────────
        var existing = await installationService.GetByInstallationCodeAsync(request.InstallationCode);
        if (existing is not null)
        {
            if (existing.IsRevoked)
            {
                logger.LogWarning("Re-enrollment denied for revoked installation. Code={Code} Id={Id}", request.InstallationCode, existing.Id);
                return StatusCode(StatusCodes.Status403Forbidden,
                    $"This installation has been revoked. Reason: {existing.RevokedReason ?? "Not specified"}.");
            }

            // Already enrolled — return existing record (idempotent re-enrollment)
            // We do NOT return the ApiKey again for security; admin must reissue if key is lost
            logger.LogInformation("Re-enrollment of existing installation. Code={Code} Id={Id}", request.InstallationCode, existing.Id);
            return Conflict(new
            {
                Message = "This InstallationCode is already registered. The original ApiKey was issued at enrollment time. " +
                          "If the key is lost, ask the Hub administrator to reissue it.",
                InstallationId = existing.Id,
                InstallationCode = existing.InstallationCode
            });
        }

        // ── New enrollment ───────────────────────────────────────────────
        var apiKey = Convert.ToHexStringLower(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        var installation = new Installation
        {
            Id = request.HintInstallationId ?? Guid.NewGuid(),
            InstallationCode = request.InstallationCode,
            Name = request.InstallationName,
            Location = request.Location,
            Components = request.Components,
            ApiKey = apiKey,
            MachineName = request.MachineName,
            OSVersion = request.OSVersion,
            DotNetVersion = request.DotNetVersion,
            AgentVersion = request.AgentVersion,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Tags = request.Tags is { Count: > 0 } t ? string.Join(",", t) : null,
            Notes = $"Auto-enrolled via EnrollmentToken on {DateTime.UtcNow:u}"
        };

        var created = await installationService.CreateAsync(installation);

        logger.LogInformation(
            "New installation enrolled: Name={Name} Code={Code} Id={Id} Location={Location}",
            created.Name, created.InstallationCode, created.Id, created.Location);

        return Ok(new EnrollmentResponse(created.Id, created.InstallationCode!, apiKey));
    }

    /// <summary>Reissue a new API key for an existing installation (admin only).</summary>
    [HttpPost("{id:guid}/reissue")]
    public async Task<IActionResult> ReissueKey(Guid id)
    {
        if (!adminAuth.IsAuthorized(Request.Headers)) return Unauthorized();

        var newKey = await installationService.ReissueApiKeyAsync(id);
        if (newKey is null) return NotFound($"Installation {id} not found.");

        logger.LogInformation("API key reissued for Installation={Id}", id);
        return Ok(new { InstallationId = id, ApiKey = newKey });
    }

    private static bool TokenEquals(string supplied, string expected)
    {
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }
}

// Request body sent by the Agent during self-enrollment
public record EnrollmentRequest(
    string EnrollmentToken,
    string InstallationCode,
    string InstallationName,
    Guid? HintInstallationId,
    string? Location,
    InstallationComponents Components,
    string? MachineName       = null,
    string? OSVersion         = null,
    string? DotNetVersion     = null,
    string? AgentVersion      = null,
    IReadOnlyList<string>? Tags = null);

// Response returned to the Agent on successful enrollment
public record EnrollmentResponse(
    Guid InstallationId,
    string InstallationCode,
    string ApiKey);

