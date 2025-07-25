using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for tenant management operations (super admin only).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ITenantContext _tenantContext;

    public TenantsController(ITenantService tenantService, ITenantContext tenantContext)
    {
        _tenantService = tenantService;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Creates a new tenant with an auto-generated admin user.
    /// </summary>
    /// <param name="createDto">Tenant creation data</param>
    /// <returns>Created tenant with admin user details</returns>
    [HttpPost]
    public async Task<ActionResult<TenantResponseDto>> CreateTenant([FromBody] CreateTenantDto createDto)
    {
        try
        {
            var result = await _tenantService.CreateTenantAsync(createDto);
            return CreatedAtAction(nameof(GetTenant), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <returns>Tenant details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<TenantResponseDto>> GetTenant(Guid id)
    {
        try
        {
            var tenant = await _tenantService.GetTenantAsync(id);
            if (tenant == null)
            {
                return NotFound($"Tenant {id} not found.");
            }
            return Ok(tenant);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Gets all tenants (super admin only).
    /// </summary>
    /// <returns>List of all tenants</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantResponseDto>>> GetAllTenants()
    {
        try
        {
            var tenants = await _tenantService.GetAllTenantsAsync();
            return Ok(tenants);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Updates tenant information.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="updateDto">Updated tenant data</param>
    /// <returns>Updated tenant details</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<TenantResponseDto>> UpdateTenant(Guid id, [FromBody] UpdateTenantDto updateDto)
    {
        try
        {
            var result = await _tenantService.UpdateTenantAsync(id, updateDto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Enables a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="reason">Reason for enabling the tenant</param>
    [HttpPost("{id}/enable")]
    public async Task<IActionResult> EnableTenant(Guid id, [FromBody] string reason = "Enabled by admin")
    {
        try
        {
            await _tenantService.SetTenantStatusAsync(id, true, reason);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Disables a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="reason">Reason for disabling the tenant</param>
    [HttpPost("{id}/disable")]
    public async Task<IActionResult> DisableTenant(Guid id, [FromBody] string reason = "Disabled by admin")
    {
        try
        {
            await _tenantService.SetTenantStatusAsync(id, false, reason);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Gets all admins for a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <returns>List of tenant admins</returns>
    [HttpGet("{id}/admins")]
    public async Task<ActionResult<IEnumerable<AdminTenantResponseDto>>> GetTenantAdmins(Guid id)
    {
        try
        {
            var admins = await _tenantService.GetTenantAdminsAsync(id);
            return Ok(admins);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Adds an admin to a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="userId">User ID to make admin</param>
    /// <param name="accessLevel">Admin access level</param>
    /// <returns>Admin tenant mapping details</returns>
    [HttpPost("{id}/admins/{userId}")]
    public async Task<ActionResult<AdminTenantResponseDto>> AddTenantAdmin(
        Guid id, 
        Guid userId, 
        [FromQuery] AdminAccessLevel accessLevel = AdminAccessLevel.TenantAdmin)
    {
        try
        {
            var result = await _tenantService.AddTenantAdminAsync(id, userId, accessLevel);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Removes an admin from a tenant.
    /// </summary>
    /// <param name="id">Tenant ID</param>
    /// <param name="userId">User ID to remove as admin</param>
    [HttpDelete("{id}/admins/{userId}")]
    public async Task<IActionResult> RemoveTenantAdmin(Guid id, Guid userId)
    {
        try
        {
            await _tenantService.RemoveTenantAdminAsync(id, userId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Forces a user to change their password on next login.
    /// </summary>
    /// <param name="id">Tenant ID (for context)</param>
    /// <param name="userId">User ID</param>
    [HttpPost("{id}/users/{userId}/force-password-change")]
    public async Task<IActionResult> ForcePasswordChange(Guid id, Guid userId)
    {
        try
        {
            await _tenantService.ForcePasswordChangeAsync(userId);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }
}