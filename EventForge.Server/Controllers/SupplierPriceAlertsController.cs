using EventForge.DTOs.Alerts;
using EventForge.Server.Filters;
using EventForge.Server.Services.Alerts;
using EventForge.Server.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for supplier price alerts management.
/// Part of FASE 5: Price Alerts System.
/// </summary>
[Route("api/v1/alerts")]
[Authorize]
[RequireLicenseFeature("ProductManagement")]
public class SupplierPriceAlertsController : BaseApiController
{
    private readonly ISupplierPriceAlertService _alertService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SupplierPriceAlertsController> _logger;

    public SupplierPriceAlertsController(
        ISupplierPriceAlertService alertService,
        ITenantContext tenantContext,
        ILogger<SupplierPriceAlertsController> logger)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets alerts with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<SupplierPriceAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<SupplierPriceAlertDto>>> GetAlerts(
        [FromQuery] AlertFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _alertService.GetAlertsAsync(filter, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts");
            return CreateInternalServerErrorProblem("An error occurred while retrieving alerts.", ex);
        }
    }

    /// <summary>
    /// Gets a single alert by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SupplierPriceAlertDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierPriceAlertDto>> GetAlert(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var alert = await _alertService.GetAlertByIdAsync(id, cancellationToken);
            if (alert == null)
            {
                return NotFound(new { message = "Alert not found" });
            }

            return Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert {AlertId}", id);
            return CreateInternalServerErrorProblem("An error occurred while retrieving the alert.", ex);
        }
    }

    /// <summary>
    /// Gets alert statistics for the current user.
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AlertStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AlertStatistics>> GetStatistics(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var stats = await _alertService.GetAlertStatisticsAsync(cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert statistics");
            return CreateInternalServerErrorProblem("An error occurred while retrieving statistics.", ex);
        }
    }

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AcknowledgeAlert(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var success = await _alertService.AcknowledgeAlertAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "Alert not found" });
            }

            return Ok(new { message = "Alert acknowledged successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
            return CreateInternalServerErrorProblem("An error occurred while acknowledging the alert.", ex);
        }
    }

    /// <summary>
    /// Resolves an alert with optional notes.
    /// </summary>
    [HttpPost("{id}/resolve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ResolveAlert(
        Guid id,
        [FromBody] ResolveAlertRequest? request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var success = await _alertService.ResolveAlertAsync(id, request?.Notes, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "Alert not found" });
            }

            return Ok(new { message = "Alert resolved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", id);
            return CreateInternalServerErrorProblem("An error occurred while resolving the alert.", ex);
        }
    }

    /// <summary>
    /// Dismisses an alert.
    /// </summary>
    [HttpPost("{id}/dismiss")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DismissAlert(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var success = await _alertService.DismissAlertAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = "Alert not found" });
            }

            return Ok(new { message = "Alert dismissed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing alert {AlertId}", id);
            return CreateInternalServerErrorProblem("An error occurred while dismissing the alert.", ex);
        }
    }

    /// <summary>
    /// Dismisses multiple alerts.
    /// </summary>
    [HttpPost("dismiss-multiple")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DismissMultipleAlerts(
        [FromBody] DismissMultipleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request?.AlertIds == null || !request.AlertIds.Any())
        {
            return BadRequest(new { message = "No alert IDs provided" });
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var count = await _alertService.DismissMultipleAlertsAsync(request.AlertIds, cancellationToken);
            return Ok(new { message = $"{count} alerts dismissed successfully", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing multiple alerts");
            return CreateInternalServerErrorProblem("An error occurred while dismissing alerts.", ex);
        }
    }

    /// <summary>
    /// Gets count of unread alerts for the current user.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> GetUnreadCount(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var count = await _alertService.GetUnreadAlertCountAsync(cancellationToken);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread alert count");
            return CreateInternalServerErrorProblem("An error occurred while retrieving unread count.", ex);
        }
    }

    /// <summary>
    /// Gets the alert configuration for the current user.
    /// </summary>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(AlertConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AlertConfigurationDto>> GetConfiguration(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var config = await _alertService.GetUserConfigurationAsync(cancellationToken);
            var dto = new AlertConfigurationDto
            {
                Id = config.Id,
                TenantId = config.TenantId,
                UserId = config.UserId,
                PriceIncreaseThresholdPercentage = config.PriceIncreaseThresholdPercentage,
                PriceDecreaseThresholdPercentage = config.PriceDecreaseThresholdPercentage,
                VolatilityThresholdPercentage = config.VolatilityThresholdPercentage,
                DaysWithoutUpdateThreshold = config.DaysWithoutUpdateThreshold,
                EnableEmailNotifications = config.EnableEmailNotifications,
                EnableBrowserNotifications = config.EnableBrowserNotifications,
                AlertOnPriceIncrease = config.AlertOnPriceIncrease,
                AlertOnPriceDecrease = config.AlertOnPriceDecrease,
                AlertOnBetterSupplier = config.AlertOnBetterSupplier,
                AlertOnVolatility = config.AlertOnVolatility,
                NotificationFrequency = config.NotificationFrequency.ToString(),
                LastDigestSentAt = config.LastDigestSentAt
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert configuration");
            return CreateInternalServerErrorProblem("An error occurred while retrieving configuration.", ex);
        }
    }

    /// <summary>
    /// Updates the alert configuration for the current user.
    /// </summary>
    [HttpPut("configuration")]
    [ProducesResponseType(typeof(AlertConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AlertConfigurationDto>> UpdateConfiguration(
        [FromBody] UpdateAlertConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var config = await _alertService.UpdateUserConfigurationAsync(request, cancellationToken);
            var dto = new AlertConfigurationDto
            {
                Id = config.Id,
                TenantId = config.TenantId,
                UserId = config.UserId,
                PriceIncreaseThresholdPercentage = config.PriceIncreaseThresholdPercentage,
                PriceDecreaseThresholdPercentage = config.PriceDecreaseThresholdPercentage,
                VolatilityThresholdPercentage = config.VolatilityThresholdPercentage,
                DaysWithoutUpdateThreshold = config.DaysWithoutUpdateThreshold,
                EnableEmailNotifications = config.EnableEmailNotifications,
                EnableBrowserNotifications = config.EnableBrowserNotifications,
                AlertOnPriceIncrease = config.AlertOnPriceIncrease,
                AlertOnPriceDecrease = config.AlertOnPriceDecrease,
                AlertOnBetterSupplier = config.AlertOnBetterSupplier,
                AlertOnVolatility = config.AlertOnVolatility,
                NotificationFrequency = config.NotificationFrequency.ToString(),
                LastDigestSentAt = config.LastDigestSentAt
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert configuration");
            return CreateInternalServerErrorProblem("An error occurred while updating configuration.", ex);
        }
    }
}

/// <summary>
/// Request to resolve an alert.
/// </summary>
public class ResolveAlertRequest
{
    public string? Notes { get; set; }
}

/// <summary>
/// Request to dismiss multiple alerts.
/// </summary>
public class DismissMultipleRequest
{
    public List<Guid> AlertIds { get; set; } = new();
}
