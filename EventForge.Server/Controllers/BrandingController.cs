using EventForge.DTOs.Configuration;
using EventForge.Server.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// API controller for branding configuration management.
/// Provides endpoints for global and tenant-specific branding settings.
/// </summary>
[Route("api/v1/branding")]
[ApiController]
public class BrandingController : BaseApiController
{
    private readonly IBrandingService _brandingService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<BrandingController> _logger;

    public BrandingController(
        IBrandingService brandingService,
        ITenantContext tenantContext,
        ILogger<BrandingController> logger)
    {
        _brandingService = brandingService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets branding configuration for the current tenant.
    /// Public endpoint that returns branding with fallback chain.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID override (for preview)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Branding configuration</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BrandingConfigurationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BrandingConfigurationDto>> GetBranding(
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        try
        {
            // Use current tenant if not specified
            var targetTenantId = tenantId ?? _tenantContext.TenantId;

            var branding = await _brandingService.GetBrandingAsync(targetTenantId, ct);

            _logger.LogDebug("Branding configuration retrieved for TenantId: {TenantId}", targetTenantId);

            return Ok(branding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branding configuration");
            return CreateInternalServerErrorProblem("Error retrieving branding configuration", ex);
        }
    }

    /// <summary>
    /// Updates global branding configuration.
    /// Requires SuperAdmin role.
    /// </summary>
    /// <param name="updateDto">Branding update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated branding configuration</returns>
    [HttpPut("global")]
    [Authorize(Policy = AuthorizationPolicies.RequireSuperAdmin)]
    [ProducesResponseType(typeof(BrandingConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BrandingConfigurationDto>> UpdateGlobalBranding(
        [FromBody] UpdateBrandingDto updateDto,
        CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var username = GetCurrentUser();
            var branding = await _brandingService.UpdateGlobalBrandingAsync(updateDto, username, ct);

            _logger.LogInformation("Global branding updated by user: {Username}", username);

            return Ok(branding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating global branding");
            return CreateInternalServerErrorProblem("Error updating global branding configuration", ex);
        }
    }

    /// <summary>
    /// Updates tenant-specific branding override.
    /// Requires Manager role for the target tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="updateDto">Branding update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated branding configuration</returns>
    [HttpPut("tenant/{tenantId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireManager)]
    [ProducesResponseType(typeof(BrandingConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BrandingConfigurationDto>> UpdateTenantBranding(
        Guid tenantId,
        [FromBody] UpdateBrandingDto updateDto,
        CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            // Validate tenant access
            if (_tenantContext.TenantId != tenantId && !_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("User {Username} attempted to update branding for unauthorized tenant {TenantId}", 
                    GetCurrentUser(), tenantId);
                return CreateForbiddenProblem("You do not have permission to update branding for this tenant.");
            }

            var username = GetCurrentUser();
            var branding = await _brandingService.UpdateTenantBrandingAsync(tenantId, updateDto, username, ct);

            _logger.LogInformation("Tenant branding updated for TenantId: {TenantId} by user: {Username}", 
                tenantId, username);

            return Ok(branding);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Tenant not found: {TenantId}", tenantId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant branding for TenantId: {TenantId}", tenantId);
            return CreateInternalServerErrorProblem("Error updating tenant branding configuration", ex);
        }
    }

    /// <summary>
    /// Deletes tenant branding override, reverting to global settings.
    /// Requires Manager role for the target tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("tenant/{tenantId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RequireManager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenantBranding(
        Guid tenantId,
        CancellationToken ct = default)
    {
        try
        {
            // Validate tenant access
            if (_tenantContext.TenantId != tenantId && !_tenantContext.IsSuperAdmin)
            {
                _logger.LogWarning("User {Username} attempted to delete branding for unauthorized tenant {TenantId}", 
                    GetCurrentUser(), tenantId);
                return CreateForbiddenProblem("You do not have permission to delete branding for this tenant.");
            }

            await _brandingService.DeleteTenantBrandingAsync(tenantId, ct);

            _logger.LogInformation("Tenant branding deleted for TenantId: {TenantId} by user: {Username}", 
                tenantId, GetCurrentUser());

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Tenant not found: {TenantId}", tenantId);
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant branding for TenantId: {TenantId}", tenantId);
            return CreateInternalServerErrorProblem("Error deleting tenant branding configuration", ex);
        }
    }

    /// <summary>
    /// Uploads a logo file for global or tenant-specific branding.
    /// Requires Manager role (or SuperAdmin for global).
    /// </summary>
    /// <param name="file">Logo file (max 5MB, formats: svg, png, jpg, jpeg, webp)</param>
    /// <param name="tenantId">Optional tenant ID for tenant-specific logo</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Uploaded logo URL</returns>
    [HttpPost("upload")]
    [Authorize(Policy = AuthorizationPolicies.RequireManager)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5MB limit
    public async Task<ActionResult<string>> UploadLogo(
        IFormFile file,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        try
        {
            // Validate tenant access if tenant-specific upload
            if (tenantId.HasValue)
            {
                if (_tenantContext.TenantId != tenantId && !_tenantContext.IsSuperAdmin)
                {
                    _logger.LogWarning("User {Username} attempted to upload logo for unauthorized tenant {TenantId}", 
                        GetCurrentUser(), tenantId);
                    return CreateForbiddenProblem("You do not have permission to upload logo for this tenant.");
                }
            }
            else
            {
                // Global upload requires SuperAdmin
                if (!_tenantContext.IsSuperAdmin)
                {
                    _logger.LogWarning("Non-SuperAdmin user {Username} attempted to upload global logo", 
                        GetCurrentUser());
                    return CreateForbiddenProblem("Only SuperAdmin can upload global logos.");
                }
            }

            var logoUrl = await _brandingService.UploadLogoAsync(file, tenantId, ct);

            _logger.LogInformation("Logo uploaded successfully: {LogoUrl} by user: {Username}", 
                logoUrl, GetCurrentUser());

            return Ok(new { logoUrl });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid logo upload attempt");
            return BadRequest(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = ex.Message,
                Instance = HttpContext.Request.Path
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo");
            return CreateInternalServerErrorProblem("Error uploading logo file", ex);
        }
    }
}
