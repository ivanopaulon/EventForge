using EventForge.DTOs.External.WhatsApp;
using EventForge.Server.Data;
using EventForge.Server.Services.External.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Controllers;

/// <summary>
/// Manages WhatsApp Business Cloud API integration configuration.
/// Read and write are restricted to Admin/SuperAdmin roles.
/// </summary>
[ApiController]
[Route("api/v1/whatsapp/config")]
[Authorize(Roles = "Admin,SuperAdmin")]
[Produces("application/json")]
public class WhatsAppConfigController(
    EventForgeDbContext dbContext,
    IWhatsAppService whatsAppService,
    IConfiguration configuration,
    ILogger<WhatsAppConfigController> logger) : BaseApiController
{
    private const string KeyPhoneNumberId = "WhatsApp:PhoneNumberId";
    private const string KeyAccessToken    = "WhatsApp:AccessToken";
    private const string KeyVerifyToken    = "WhatsApp:VerifyToken";
    private const string KeyApiVersion     = "WhatsApp:ApiVersion";
    private const string KeyEnabled        = "WhatsApp:Enabled";
    private const string CategoryWhatsApp  = "WhatsApp";

    /// <summary>Returns the current WhatsApp configuration (AccessToken is masked).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(WhatsAppConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WhatsAppConfigDto>> GetConfig(CancellationToken cancellationToken)
    {
        try
        {
            var configs = await dbContext.SystemConfigurations
                .AsNoTracking()
                .Where(c => c.Category == CategoryWhatsApp && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            string Get(string key, string fallback) =>
                configs.FirstOrDefault(c => c.Key == key)?.Value
                ?? configuration[key]
                ?? fallback;

            var accessToken = Get(KeyAccessToken, string.Empty);
            var maskedToken = accessToken.Length > 4
                ? new string('•', accessToken.Length - 4) + accessToken[^4..]
                : new string('•', accessToken.Length);

            return Ok(new WhatsAppConfigDto
            {
                PhoneNumberId = Get(KeyPhoneNumberId, string.Empty),
                AccessToken   = maskedToken,
                VerifyToken   = Get(KeyVerifyToken, string.Empty),
                ApiVersion    = Get(KeyApiVersion, "v19.0"),
                IsEnabled     = bool.TryParse(Get(KeyEnabled, "false"), out var enabled) && enabled
            });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error reading WhatsApp configuration", ex);
        }
    }

    /// <summary>
    /// Saves the WhatsApp configuration.
    /// Send the full AccessToken to update it; send the masked value (or empty) to leave it unchanged.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SaveConfig([FromBody] WhatsAppConfigDto dto, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid) return CreateValidationProblemDetails();

            var user = GetCurrentUser();
            var now  = DateTime.UtcNow;

            var keysToSave = new Dictionary<string, string>
            {
                [KeyPhoneNumberId] = dto.PhoneNumberId,
                [KeyApiVersion]    = dto.ApiVersion,
                [KeyVerifyToken]   = dto.VerifyToken,
                [KeyEnabled]       = dto.IsEnabled.ToString().ToLower()
            };

            // Only update AccessToken when the caller sends a real value (not masked)
            if (!string.IsNullOrWhiteSpace(dto.AccessToken) && !dto.AccessToken.Contains('•'))
                keysToSave[KeyAccessToken] = dto.AccessToken;

            foreach (var (key, value) in keysToSave)
            {
                var existing = await dbContext.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == key && c.Category == CategoryWhatsApp, cancellationToken);

                if (existing == null)
                {
                    dbContext.SystemConfigurations.Add(new EventForge.Server.Data.Entities.Configuration.SystemConfiguration
                    {
                        Key         = key,
                        Value       = value,
                        Category    = CategoryWhatsApp,
                        Description = $"WhatsApp integration: {key}",
                        IsEncrypted = key == KeyAccessToken,
                        CreatedBy   = user,
                        TenantId    = Guid.Empty  // system-wide setting
                    });
                }
                else
                {
                    existing.Value      = value;
                    existing.ModifiedAt = now;
                    existing.ModifiedBy = user;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("WhatsApp config updated by {User}", user);
            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error saving WhatsApp configuration", ex);
        }
    }

    /// <summary>
    /// Tests the WhatsApp connection by sending a ping to the Meta Graph API.
    /// Returns 200 + a status message.
    /// </summary>
    [HttpPost("test")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        try
        {
            var ok = await whatsAppService.TestConnectionAsync(cancellationToken);
            if (ok)
                return Ok(new { success = true, message = "Connessione a WhatsApp Business API riuscita." });
            else
                return Ok(new { success = false, message = "Connessione fallita. Verifica PhoneNumberId e AccessToken." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing WhatsApp connection");
            return Ok(new { success = false, message = $"Errore durante il test: {ex.Message}" });
        }
    }
}
