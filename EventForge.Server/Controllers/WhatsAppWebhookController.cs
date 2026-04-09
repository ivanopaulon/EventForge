using EventForge.DTOs.Chat;
using EventForge.DTOs.External.WhatsApp;
using EventForge.Server.Services.External.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Handles WhatsApp Cloud API webhook verification and inbound message processing.
/// </summary>
[ApiController]
[Route("api/whatsapp/webhook")]
[Produces("application/json")]
public class WhatsAppWebhookController(
    IWhatsAppConversazioneService whatsAppConversazioneService,
    IConfiguration configuration,
    ILogger<WhatsAppWebhookController> logger,
    ITenantContext tenantContext) : BaseApiController
{
    private readonly string _verifyToken = configuration["WhatsApp:VerifyToken"] ?? string.Empty;

    /// <summary>GET endpoint for Meta webhook verification challenge.</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode == "subscribe" && verifyToken == _verifyToken)
        {
            logger.LogInformation("WhatsApp webhook verified successfully");
            return Ok(challenge);
        }
        logger.LogWarning("WhatsApp webhook verification failed — mode={Mode}, token match={Match}", mode, verifyToken == _verifyToken);
        return Forbid();
    }

    /// <summary>POST endpoint for inbound WhatsApp messages/status updates. Returns 200 immediately.</summary>
    [HttpPost]
    [AllowAnonymous]
    public IActionResult ReceiveMessage([FromBody] WhatsAppInboundPayloadDto payload)
    {
        if (!tenantContext.CurrentTenantId.HasValue)
        {
            logger.LogWarning("WhatsApp webhook received but no tenant context available");
            return Ok();
        }
        var tenantId = tenantContext.CurrentTenantId.Value;

        // Fire-and-forget: respond 200 immediately as required by Meta
        _ = Task.Run(async () =>
        {
            try
            {
                foreach (var entry in payload.Entry ?? [])
                {
                    foreach (var change in entry.Changes ?? [])
                    {
                        var value = change.Value;
                        if (value == null) continue;

                        foreach (var msg in value.Messages ?? [])
                        {
                            if (msg.Type != "text" || msg.Text?.Body == null) continue;
                            if (!long.TryParse(msg.Timestamp, out var unixTs))
                            {
                                logger.LogWarning("Invalid WhatsApp message timestamp: {Ts}", msg.Timestamp);
                                continue;
                            }
                            var timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTs).UtcDateTime;
                            await whatsAppConversazioneService.GestisciMessaggioEntranteAsync(
                                msg.From, msg.Text.Body, msg.Id, timestamp, tenantId);
                        }

                        foreach (var status in value.Statuses ?? [])
                        {
                            var stato = status.Status switch
                            {
                                "delivered" => WhatsAppDeliveryStatus.Consegnato,
                                "read"      => WhatsAppDeliveryStatus.Letto,
                                _           => WhatsAppDeliveryStatus.Inviato
                            };
                            await whatsAppConversazioneService.AggiornaStatoMessaggioAsync(status.Id, stato, tenantId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing WhatsApp webhook payload");
            }
        });

        return Ok();
    }
}
