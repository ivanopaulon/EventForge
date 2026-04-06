using EventForge.UpdateHub.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.UpdateHub.Controllers;

/// <summary>
/// Agent self-enrollment endpoint.
/// Allows a new Agent to request its own API key without admin intervention,
/// provided it presents the correct EnrollmentToken configured on the Hub.
/// </summary>
[ApiController]
[Route("api/v1/enrollments")]
public class EnrollmentsController(
    IInstallationService installationService,
    UpdateHubOptions hubOptions,
    ILogger<EnrollmentsController> logger) : ControllerBase
{
    /// <summary>
    /// Request a new API key for a fresh Agent installation.
    /// The Agent must supply the shared EnrollmentToken set on the Hub.
    /// On success the Hub creates an Installation record and returns the ApiKey
    /// together with the confirmed InstallationId.
    /// The Agent is expected to persist these values in its appsettings.json.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollmentRequest request)
    {
        if (!hubOptions.AllowAutoEnrollment || string.IsNullOrWhiteSpace(hubOptions.EnrollmentToken))
        {
            logger.LogWarning("Enrollment attempt rejected — auto-enrollment is disabled. InstallationName={Name}", request.InstallationName);
            return StatusCode(StatusCodes.Status403Forbidden,
                "Auto-enrollment is disabled on this Hub. Ask the administrator to register the installation manually.");
        }

        if (request.EnrollmentToken != hubOptions.EnrollmentToken)
        {
            logger.LogWarning("Enrollment attempt with invalid token. InstallationName={Name}", request.InstallationName);
            return Unauthorized("Invalid enrollment token.");
        }

        // If the agent already has an InstallationId, check whether it exists so we don't create duplicates.
        if (request.InstallationId.HasValue && request.InstallationId != Guid.Empty)
        {
            var existing = await installationService.GetByIdAsync(request.InstallationId.Value);
            if (existing is not null)
            {
                logger.LogInformation("Enrollment: installation already exists, returning existing record. Id={Id}", existing.Id);
                // We don't expose the ApiKey of an existing installation for security reasons.
                return Conflict(new
                {
                    Message = "An installation with this ID already exists. Contact the administrator if you need the API key reissued.",
                    InstallationId = existing.Id
                });
            }
        }

        var apiKey = Convert.ToHexString(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLower();

        var installation = new Installation
        {
            Id = request.InstallationId ?? Guid.NewGuid(),
            Name = request.InstallationName,
            Location = request.Location,
            Components = request.Components,
            ApiKey = apiKey,
            Notes = "Auto-enrolled via EnrollmentToken"
        };

        var created = await installationService.CreateAsync(installation);

        logger.LogInformation(
            "New installation enrolled: Name={Name} Id={Id} Location={Location}",
            created.Name, created.Id, created.Location);

        return Ok(new EnrollmentResponse(created.Id, apiKey));
    }
}

// Request body sent by the Agent during self-enrollment
public record EnrollmentRequest(
    string EnrollmentToken,
    string InstallationName,
    Guid? InstallationId,
    string? Location,
    InstallationComponents Components);

// Response returned to the Agent on successful enrollment
public record EnrollmentResponse(
    Guid InstallationId,
    string ApiKey);
