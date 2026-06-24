using EventForge.Server.Services.External.AI;
using EventForge.Server.Services.External.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.AI;
using System.Text.Json;

namespace EventForge.Server.Controllers;

/// <summary>
/// Developer tooling: simulates inbound WhatsApp messages through the full AI order pipeline.
/// Only accessible to Admin/SuperAdmin and only in non-production environments (or when the
/// <c>EnableWhatsAppSimulation</c> feature flag is set to <c>true</c>).
/// </summary>
[ApiController]
[Route("api/v1/whatsapp")]
[Authorize(Roles = "Admin,SuperAdmin")]
[Produces("application/json")]
public class WhatsAppAiTestController(
    IWhatsAppConversazioneService whatsAppConversazioneService,
    IAIOrderService aiOrderService,
    IOrderAIContextBuilder aiContextBuilder,
    IWhatsAppOrderService whatsAppOrderService,
    IConfiguration configuration,
    IWebHostEnvironment env,
    ITenantContext tenantContext,
    ILogger<WhatsAppAiTestController> logger) : BaseApiController
{
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Simulates an inbound WhatsApp message through the full AI pipeline.
    /// Returns a detailed debug response including intent, draft state, and AI reply.
    /// Only available in Development/Staging or when <c>EnableWhatsAppSimulation=true</c>.
    /// </summary>
    [HttpPost("simulate-inbound")]
    [ProducesResponseType(typeof(SimulateInboundResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SimulateInboundResultDto>> SimulateInbound(
        [FromBody] SimulateInboundDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (!IsSimulationAllowed())
            return CreateForbiddenProblem("WhatsApp simulation is only available in non-production environments or when EnableWhatsAppSimulation is true.");

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation != null) return tenantValidation;
        var tenantId = tenantContext.CurrentTenantId!.Value;

        try
        {
            // Use a fake message ID so the simulation does not clash with real messages
            var fakeMessageId = $"sim_{Guid.NewGuid():N}";
            var timestamp = DateTime.UtcNow;

            // Run the same ingest path as the real webhook
            await whatsAppConversazioneService.GestisciMessaggioEntranteAsync(
                request.PhoneNumber,
                request.MessageText,
                fakeMessageId,
                timestamp,
                tenantId,
                cancellationToken);

            // Retrieve the thread that was created/updated
            var thread = await whatsAppConversazioneService.GetOrCreateConversazioneAsync(
                request.PhoneNumber, tenantId, cancellationToken);

            // Load session state for debug output
            var session = await whatsAppOrderService.GetOrCreateSessionAsync(
                thread.Id, thread.BusinessPartyId, tenantId, cancellationToken);

            // Classify intent for the debug panel (re-run — lightweight)
            MessageIntent intent;
            try
            {
                intent = await aiOrderService.ClassifyIntentAsync(
                    request.MessageText, session.DraftJson, cancellationToken);
            }
            catch
            {
                intent = MessageIntent.Altro;
            }

            // Parse draft items for the debug panel
            var draftItems = new List<OrderDraftItem>();
            if (!string.IsNullOrWhiteSpace(session.DraftJson))
            {
                try { draftItems = JsonSerializer.Deserialize<List<OrderDraftItem>>(session.DraftJson, _jsonOpts) ?? []; }
                catch { /* ignore parse errors in debug output */ }
            }

            var result = new SimulateInboundResultDto
            {
                ChatThreadId = thread.Id,
                DetectedIntent = intent,
                SessionState = (OrderConversationState)(int)session.State,
                CurrentDraft = draftItems,
                DocumentCreated = session.CreatedDocumentHeaderId.HasValue,
                DocumentHeaderId = session.CreatedDocumentHeaderId
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "simulate-inbound failed for {Phone}", request.PhoneNumber);
            return CreateInternalServerErrorProblem("Simulation failed.", ex);
        }
    }

    /// <summary>
    /// Resets the AI order conversation session for a given phone number (dev tooling).
    /// </summary>
    [HttpPost("simulate-reset-session")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetSession(
        [FromQuery] string phoneNumber,
        CancellationToken cancellationToken)
    {
        if (!IsSimulationAllowed())
            return CreateForbiddenProblem("WhatsApp simulation is only available in non-production environments or when EnableWhatsAppSimulation is true.");

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation != null) return tenantValidation;
        var tenantId = tenantContext.CurrentTenantId!.Value;

        try
        {
            var thread = await whatsAppConversazioneService.GetOrCreateConversazioneAsync(
                phoneNumber, tenantId, cancellationToken);

            var session = await whatsAppOrderService.GetOrCreateSessionAsync(
                thread.Id, thread.BusinessPartyId, tenantId, cancellationToken);

            await whatsAppOrderService.UpdateSessionAsync(
                session, OrderConversationState.Idle, null, cancellationToken);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "simulate-reset-session failed for {Phone}", phoneNumber);
            return CreateInternalServerErrorProblem("Reset failed.", ex);
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private bool IsSimulationAllowed()
    {
        if (!env.IsProduction()) return true;
        return configuration.GetValue<bool>("EnableWhatsAppSimulation", false);
    }
}
