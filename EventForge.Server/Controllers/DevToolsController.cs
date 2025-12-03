using EventForge.DTOs.DevTools;
using EventForge.Server.Services.DevTools;
using EventForge.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller per strumenti di sviluppo.
/// Accessibile solo in ambiente di sviluppo o quando esplicitamente abilitato.
/// </summary>
[Route("api/v1/devtools")]
[Authorize]
[ApiController]
public class DevToolsController : BaseApiController
{
    private readonly IProductGeneratorService _productGeneratorService;
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevToolsController> _logger;

    public DevToolsController(
        IProductGeneratorService productGeneratorService,
        ITenantContext tenantContext,
        IConfiguration configuration,
        ILogger<DevToolsController> logger)
    {
        _productGeneratorService = productGeneratorService ?? throw new ArgumentNullException(nameof(productGeneratorService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Avvia la generazione di prodotti di test.
    /// Accessibile solo ad amministratori e solo in ambiente di sviluppo o quando DEVTOOLS_ENABLED=true.
    /// </summary>
    /// <param name="request">Parametri di generazione</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Informazioni sul job avviato</returns>
    /// <response code="200">Job avviato con successo</response>
    /// <response code="400">Richiesta non valida</response>
    /// <response code="403">Accesso negato (non admin o devtools non abilitati)</response>
    [HttpPost("generate-products")]
    [ProducesResponseType(typeof(GenerateProductsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GenerateProductsResponseDto>> GenerateProducts(
        [FromBody] GenerateProductsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Verifica che i devtools siano abilitati
        if (!IsDevToolsEnabled())
        {
            _logger.LogWarning("Tentativo di accesso a devtools quando non abilitati. Utente: {User}",
                User.Identity?.Name ?? "Unknown");
            return Forbid();
        }

        // Verifica che l'utente sia admin
        if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            _logger.LogWarning("Tentativo di accesso a devtools da utente non admin. Utente: {User}",
                User.Identity?.Name ?? "Unknown");
            return Forbid();
        }

        // Valida il tenant
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        var tenantId = _tenantContext.CurrentTenantId!.Value;
        var userId = _tenantContext.CurrentUserId!.Value;

        try
        {
            var jobId = await _productGeneratorService.StartGenerationJobAsync(request, tenantId, userId, cancellationToken);

            _logger.LogInformation("Job di generazione prodotti avviato. JobId: {JobId}, Count: {Count}, TenantId: {TenantId}, UserId: {UserId}",
                jobId, request.Count, tenantId, userId);

            var response = new GenerateProductsResponseDto
            {
                JobId = jobId,
                Message = $"Job di generazione di {request.Count} prodotti avviato con successo.",
                StartedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'avvio del job di generazione prodotti");
            return Problem(
                title: "Errore nell'avvio del job",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Ottiene lo stato di un job di generazione prodotti.
    /// </summary>
    /// <param name="jobId">ID del job</param>
    /// <returns>Stato del job</returns>
    /// <response code="200">Stato del job</response>
    /// <response code="403">Accesso negato</response>
    /// <response code="404">Job non trovato</response>
    [HttpGet("generate-products/status/{jobId}")]
    [ProducesResponseType(typeof(GenerateProductsStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<GenerateProductsStatusDto> GetGenerateProductsStatus(string jobId)
    {
        // Verifica che i devtools siano abilitati
        if (!IsDevToolsEnabled())
        {
            return Forbid();
        }

        // Verifica che l'utente sia admin
        if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            return Forbid();
        }

        var status = _productGeneratorService.GetJobStatus(jobId);
        if (status == null)
        {
            return NotFound(new { message = $"Job con ID {jobId} non trovato." });
        }

        return Ok(status);
    }

    /// <summary>
    /// Cancella un job di generazione prodotti in esecuzione.
    /// </summary>
    /// <param name="jobId">ID del job da cancellare</param>
    /// <returns>Risultato della cancellazione</returns>
    /// <response code="200">Job cancellato con successo</response>
    /// <response code="403">Accesso negato</response>
    /// <response code="404">Job non trovato o non cancellabile</response>
    [HttpPost("generate-products/cancel/{jobId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult CancelGenerateProducts(string jobId)
    {
        // Verifica che i devtools siano abilitati
        if (!IsDevToolsEnabled())
        {
            return Forbid();
        }

        // Verifica che l'utente sia admin
        if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
        {
            return Forbid();
        }

        var cancelled = _productGeneratorService.CancelJob(jobId);
        if (!cancelled)
        {
            return NotFound(new { message = $"Job con ID {jobId} non trovato o non può essere cancellato." });
        }

        _logger.LogInformation("Job {JobId} cancellato dall'utente {User}", jobId, User.Identity?.Name ?? "Unknown");

        return Ok(new { message = "Job cancellato con successo." });
    }

    /// <summary>
    /// Endpoint semplificato per la generazione di prodotti di test.
    /// Sempre visibile, richiede autenticazione Admin/SuperAdmin.
    /// </summary>
    /// <param name="request">Payload con count</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risposta con informazioni sul job avviato</returns>
    [HttpPost("/api/devtools/generate-test-products")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateTestProducts(
        [FromBody] GenerateTestProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!User?.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized();
        }

        // Valida il tenant
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        var tenantId = _tenantContext.CurrentTenantId!.Value;
        var userId = _tenantContext.CurrentUserId!.Value;

        try
        {
            // Se esiste il servizio IProductGeneratorService, lo usa
            var fullRequest = new GenerateProductsRequestDto
            {
                Count = request.Count,
                BatchSize = 100 // Default batch size
            };

            var jobId = await _productGeneratorService.StartGenerationJobAsync(fullRequest, tenantId, userId, cancellationToken);

            _logger.LogInformation("Job di generazione prodotti di test avviato. JobId: {JobId}, Count: {Count}, TenantId: {TenantId}, UserId: {UserId}",
                jobId, request.Count, tenantId, userId);

            return Accepted(new { started = true, jobId, count = request.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nell'avvio del job di generazione prodotti di test");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Verifica se i devtools sono abilitati.
    /// I devtools sono abilitati se:
    /// - DEVTOOLS_ENABLED è impostato a "true" nelle variabili d'ambiente
    /// - OPPURE se l'ambiente è Development
    /// </summary>
    private bool IsDevToolsEnabled()
    {
        var devToolsEnabled = _configuration.GetValue<string>("DEVTOOLS_ENABLED");
        var environment = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");

        return devToolsEnabled?.Equals("true", StringComparison.OrdinalIgnoreCase) == true ||
               environment?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true;
    }
}

/// <summary>
/// Request per la generazione semplificata di prodotti di test
/// </summary>
public class GenerateTestProductsRequest
{
    public int Count { get; set; }
}
