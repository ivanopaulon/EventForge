using EventForge.DTOs.PaymentTerminal;
using EventForge.Server.Services.PaymentTerminal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

[Route("api/v1/payment-terminals")]
[Authorize]
public class PaymentTerminalController(
    IPaymentTerminalService paymentTerminalService,
    ITenantContext tenantContext) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<PaymentTerminalDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PaymentTerminalDto>>> GetAll(CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            return Ok(await paymentTerminalService.GetAllAsync(ct));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving payment terminals.", ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentTerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentTerminalDto>> GetById(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await paymentTerminalService.GetByIdAsync(id, ct);
            if (result is null) return CreateNotFoundProblem($"Payment terminal {id} not found.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving payment terminal.", ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentTerminalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
            return CreateInternalServerErrorProblem("Error creating payment terminal.", ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PaymentTerminalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentTerminalDto>> Update(Guid id, [FromBody] UpdatePaymentTerminalDto dto, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await paymentTerminalService.UpdateAsync(id, dto, GetCurrentUser(), ct);
            if (result is null) return CreateNotFoundProblem($"Payment terminal {id} not found.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating payment terminal.", ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var deleted = await paymentTerminalService.DeleteAsync(id, GetCurrentUser(), ct);
            if (!deleted) return CreateNotFoundProblem($"Payment terminal {id} not found.");
            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error deleting payment terminal.", ex);
        }
    }

    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
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
            return CreateInternalServerErrorProblem("Error sending payment.", ex);
        }
    }

    [HttpPost("{id:guid}/void")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentResultDto>> SendVoid(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            return Ok(await paymentTerminalService.SendVoidAsync(id, ct));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error sending void.", ex);
        }
    }

    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
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
            return CreateInternalServerErrorProblem("Error sending refund.", ex);
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
            return Ok(new { message = "Connection successful." });
        }
        catch (Exception ex)
        {
            return CreateServiceUnavailableProblem($"Connection test failed: {ex.Message}");
        }
    }

    [HttpPost("test-tcp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestTcp([FromQuery] string host, [FromQuery] int port, [FromQuery] int timeoutMs = 5000, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            await paymentTerminalService.TestTcpConnectionAsync(host, port, timeoutMs, ct);
            return Ok(new { message = "Connection successful." });
        }
        catch (Exception ex)
        {
            return CreateServiceUnavailableProblem($"TCP test failed: {ex.Message}");
        }
    }

    [HttpPost("test-tcp-via-agent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestTcpViaAgent([FromQuery] string agentBaseUrl, [FromQuery] string host, [FromQuery] int port, [FromQuery] int timeoutMs = 5000, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            await paymentTerminalService.TestTcpViaAgentAsync(agentBaseUrl, host, port, timeoutMs, ct);
            return Ok(new { message = "Connection successful." });
        }
        catch (Exception ex)
        {
            return CreateServiceUnavailableProblem($"Agent TCP test failed: {ex.Message}");
        }
    }
}
