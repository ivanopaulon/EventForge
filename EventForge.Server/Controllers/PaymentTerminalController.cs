using Prym.DTOs.PaymentTerminal;
using EventForge.Server.Services.PaymentTerminal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for Protocol 17 (ECR17) POS payment terminal operations.
/// Supports CRUD management and payment transaction commands (pay, void, refund).
/// All operations require the <c>RequireStoreConfig</c> policy.
/// </summary>
[Route("api/v1/payment-terminals")]
[Authorize(Policy = "RequireStoreConfig")]
public class PaymentTerminalController(
    IPaymentTerminalService paymentTerminalService,
    ITenantContext tenantContext,
    IConfiguration configuration,
    ILogger<PaymentTerminalController> logger) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentTerminalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<PaymentTerminalDto>>> GetAll(CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            return Ok(await paymentTerminalService.GetAllAsync(ct));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Errore nel recupero dei terminali di pagamento.", ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentTerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentTerminalDto>> GetById(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await paymentTerminalService.GetByIdAsync(id, ct);
            if (result is null) return CreateNotFoundProblem($"Terminale di pagamento {id} non trovato.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Errore nel recupero del terminale di pagamento.", ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentTerminalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentTerminalDto>> Create([FromBody] CreatePaymentTerminalDto dto, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await paymentTerminalService.CreateAsync(dto, GetCurrentUser(), ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Errore nella creazione del terminale di pagamento.", ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PaymentTerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentTerminalDto>> Update(Guid id, [FromBody] UpdatePaymentTerminalDto dto, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await paymentTerminalService.UpdateAsync(id, dto, GetCurrentUser(), ct);
            if (result is null) return CreateNotFoundProblem($"Terminale di pagamento {id} non trovato.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Errore nell'aggiornamento del terminale di pagamento.", ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var deleted = await paymentTerminalService.DeleteAsync(id, GetCurrentUser(), ct);
            if (!deleted) return CreateNotFoundProblem($"Terminale di pagamento {id} non trovato.");
            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Errore nell'eliminazione del terminale di pagamento.", ex);
        }
    }

    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResultDto>> SendPayment(Guid id, [FromBody] PaymentRequestDto request, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            return Ok(await paymentTerminalService.SendPaymentAsync(id, request, ct));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Errore nell'invio del pagamento.", ex);
        }
    }

    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResultDto>> SendVoid(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            return Ok(await paymentTerminalService.SendVoidAsync(id, ct));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Errore nell'invio dello storno.", ex);
        }
    }

    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResultDto>> SendRefund(Guid id, [FromBody] PaymentRequestDto request, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            return Ok(await paymentTerminalService.SendRefundAsync(id, request, ct));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Errore nell'invio del rimborso.", ex);
        }
    }

    [HttpPost("{id:guid}/test-connection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            await paymentTerminalService.TestConnectionAsync(id, ct);
            return Ok(new { message = "Connessione riuscita." });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TestConnection failed for terminal {Id}", id);
            return CreateServiceUnavailableProblem($"Test di connessione fallito: {ex.Message}");
        }
    }

    [HttpPost("test-tcp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestTcp([FromQuery] string host, [FromQuery] int port, [FromQuery] int timeoutMs = 5000, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (string.IsNullOrWhiteSpace(host))
            return CreateValidationProblemDetails("Il parametro 'host' è obbligatorio.");
        if (port is < 1 or > 65535)
            return CreateValidationProblemDetails("La porta deve essere compresa tra 1 e 65535.");
        try
        {
            await paymentTerminalService.TestTcpConnectionAsync(host, port, timeoutMs, ct);
            return Ok(new { host, port, message = "Connessione riuscita." });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TestTcp failed for {Host}:{Port}", host, port);
            return CreateServiceUnavailableProblem($"Test TCP fallito: {ex.Message}");
        }
    }

    [HttpPost("test-tcp-via-agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestTcpViaAgent([FromQuery] Guid agentId, [FromQuery] string host, [FromQuery] int port, [FromQuery] int timeoutMs = 5000, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (string.IsNullOrWhiteSpace(host))
            return CreateValidationProblemDetails("Il parametro 'host' è obbligatorio.");
        if (port is < 1 or > 65535)
            return CreateValidationProblemDetails("La porta deve essere compresa tra 1 e 65535.");

        var agentUrl = configuration[$"AgentProxies:{agentId}"];
        if (string.IsNullOrWhiteSpace(agentUrl))
            return CreateNotFoundProblem($"Agente '{agentId}' non configurato. Aggiungere 'AgentProxies:{agentId}' alla configurazione.");

        try
        {
            await paymentTerminalService.TestTcpViaAgentAsync(agentUrl, host, port, timeoutMs, ct);
            return Ok(new { agentId, host, port, message = "Connessione riuscita." });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TestTcpViaAgent failed for agent {AgentId} {Host}:{Port}", agentId, host, port);
            return CreateServiceUnavailableProblem($"Test TCP via agente fallito: {ex.Message}");
        }
    }
}
