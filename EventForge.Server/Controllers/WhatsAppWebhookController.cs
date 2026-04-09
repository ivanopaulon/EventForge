using EventForge.DTOs.External.WhatsApp;
using EventForge.Server.Services.External.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Webhook endpoint for Meta WhatsApp Cloud API events.
/// <para>
/// Two endpoints are exposed:
/// <list type="bullet">
///   <item>
///     <c>GET  /api/v1/whatsapp/webhook</c> — Verification challenge (called once by Meta when
///     the webhook URL is registered in the developer portal).
///   </item>
///   <item>
///     <c>POST /api/v1/whatsapp/webhook</c> — Event delivery (called by Meta for every incoming
///     message, status update, etc.).
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Meta Developer Portal setup (test phase)</b>
/// <list type="number">
///   <item>Create a Meta App of type <em>Business</em> at https://developers.facebook.com</item>
///   <item>Add the <em>WhatsApp</em> product to the app.</item>
///   <item>
///     Under <em>WhatsApp → Configuration → Webhook</em>, set:
///     <list type="bullet">
///       <item>Callback URL: <c>https://&lt;your-host&gt;/api/v1/whatsapp/webhook</c></item>
///       <item>Verify Token: the value configured in <c>WhatsApp:VerifyToken</c> (appsettings).</item>
///     </list>
///   </item>
///   <item>Subscribe to the <em>messages</em> field.</item>
///   <item>
///     Generate a temporary access token (or configure a System User token) and store it in
///     <c>WhatsApp:AccessToken</c>. Also copy the <em>Phone Number ID</em> into
///     <c>WhatsApp:PhoneNumberId</c>.
///   </item>
/// </list>
/// </para>
/// </summary>
[Route("api/v1/whatsapp/webhook")]
[AllowAnonymous]
public class WhatsAppWebhookController(
    IWhatsAppService whatsAppService,
    ILogger<WhatsAppWebhookController> logger) : BaseApiController
{
    /// <summary>
    /// Handles the Meta webhook verification challenge.
    /// Meta sends a GET request with three query parameters; the server must reply with
    /// the <c>hub.challenge</c> value if the token matches.
    /// </summary>
    /// <param name="mode">Expected value: "subscribe".</param>
    /// <param name="verifyToken">Must match <c>WhatsApp:VerifyToken</c> in configuration.</param>
    /// <param name="challenge">Opaque string that must be echoed back verbatim.</param>
    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")]         string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")]    string? challenge)
    {
        if (string.IsNullOrWhiteSpace(mode)
            || string.IsNullOrWhiteSpace(verifyToken)
            || string.IsNullOrWhiteSpace(challenge))
        {
            logger.LogWarning(
                "WhatsApp webhook GET received without required query parameters.");
            return CreateForbiddenProblem("Missing verification parameters.");
        }

        var result = whatsAppService.VerifyWebhook(mode, verifyToken, challenge);
        if (result is null)
            return CreateForbiddenProblem("Webhook verification failed.");

        // Meta requires the challenge echoed as plain text (not JSON).
        return Content(result, "text/plain");
    }

    /// <summary>
    /// Receives and processes Meta webhook event notifications.
    /// </summary>
    /// <param name="payload">Webhook payload deserialized from the request body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP 200 OK — Meta requires a 200 response within 20 seconds.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReceiveWebhook(
        [FromBody] WhatsAppWebhookPayload payload,
        CancellationToken cancellationToken = default)
    {
        if (payload is null)
        {
            logger.LogWarning("WhatsApp webhook POST received with empty body.");
            // Still return 200 to prevent Meta from retrying.
            return Ok();
        }

        try
        {
            var orders = await whatsAppService.ProcessWebhookAsync(payload, cancellationToken);

            if (orders.Count > 0)
            {
                logger.LogInformation(
                    "WhatsApp webhook processed: {OrderCount} order(s) received.",
                    orders.Count);
            }

            // Always return 200 — Meta will retry if it receives any other status code.
            return Ok();
        }
        catch (Exception ex)
        {
            // Log but still return 200 to avoid endless Meta retries.
            logger.LogError(ex, "Unhandled error while processing WhatsApp webhook.");
            return Ok();
        }
    }
}
