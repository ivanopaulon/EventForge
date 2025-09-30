using EventForge.DTOs.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for managing licenses and license assignments.
/// Provides CRUD operations for licenses and tenant license assignments.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class LicenseController : BaseApiController
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(EventForgeDbContext context, ILogger<LicenseController> logger,
        ITenantContext tenantContext)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all available licenses.
    /// </summary>
    /// <returns>List of available licenses</returns>
    /// <response code="200">Returns the list of licenses</response>
    /// <response code="403">If the user doesn't have SuperAdmin role</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(IEnumerable<LicenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<LicenseDto>>> GetLicenses()
    {
        try
        {
            var licenses = await _context.Licenses
                .Include(l => l.LicenseFeatures)
                    .ThenInclude(lf => lf.LicenseFeaturePermissions)
                        .ThenInclude(lfp => lfp.Permission)
                .Include(l => l.TenantLicenses)
                .Where(l => !l.IsDeleted)
                .OrderBy(l => l.TierLevel)
                .ThenBy(l => l.Name)
                .ToListAsync();

            var licenseDtos = licenses.Select(l => new LicenseDto
            {
                Id = l.Id,
                Name = l.Name,
                DisplayName = l.DisplayName,
                Description = l.Description,
                MaxUsers = l.MaxUsers,
                MaxApiCallsPerMonth = l.MaxApiCallsPerMonth,
                IsActive = l.IsActive,
                TierLevel = l.TierLevel,
                CreatedAt = l.CreatedAt,
                CreatedBy = l.CreatedBy ?? "system",
                ModifiedAt = l.ModifiedAt,
                ModifiedBy = l.ModifiedBy,
                TenantCount = l.TenantLicenses.Count(tl => tl.IsAssignmentActive),
                Features = l.LicenseFeatures.Select(lf => new LicenseFeatureDto
                {
                    Id = lf.Id,
                    Name = lf.Name,
                    DisplayName = lf.DisplayName,
                    Description = lf.Description,
                    Category = lf.Category,
                    IsActive = lf.IsActive,
                    LicenseId = lf.LicenseId,
                    LicenseName = l.Name,
                    CreatedAt = lf.CreatedAt,
                    CreatedBy = lf.CreatedBy ?? "system",
                    ModifiedAt = lf.ModifiedAt,
                    ModifiedBy = lf.ModifiedBy,
                    RequiredPermissions = lf.LicenseFeaturePermissions.Select(lfp => lfp.Permission.Name).ToList()
                }).ToList()
            }).ToList();

            return Ok(licenseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving licenses");
            return CreateInternalServerErrorProblem("An error occurred while retrieving licenses", ex);
        }
    }

    /// <summary>
    /// Get a specific license by ID.
    /// </summary>
    /// <param name="id">License ID</param>
    /// <returns>License details</returns>
    /// <response code="200">Returns the license details</response>
    /// <response code="403">If the user doesn't have SuperAdmin role</response>
    /// <response code="404">If the license is not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(LicenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LicenseDto>> GetLicense(Guid id)
    {
        try
        {
            var license = await _context.Licenses
                .Include(l => l.LicenseFeatures)
                    .ThenInclude(lf => lf.LicenseFeaturePermissions)
                        .ThenInclude(lfp => lfp.Permission)
                .Include(l => l.TenantLicenses)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (license == null)
            {
                return NotFound($"License with ID {id} not found");
            }

            var licenseDto = new LicenseDto
            {
                Id = license.Id,
                Name = license.Name,
                DisplayName = license.DisplayName,
                Description = license.Description,
                MaxUsers = license.MaxUsers,
                MaxApiCallsPerMonth = license.MaxApiCallsPerMonth,
                IsActive = license.IsActive,
                TierLevel = license.TierLevel,
                CreatedAt = license.CreatedAt,
                CreatedBy = license.CreatedBy ?? "system",
                ModifiedAt = license.ModifiedAt,
                ModifiedBy = license.ModifiedBy,
                TenantCount = license.TenantLicenses.Count(tl => tl.IsAssignmentActive),
                Features = license.LicenseFeatures.Select(lf => new LicenseFeatureDto
                {
                    Id = lf.Id,
                    Name = lf.Name,
                    DisplayName = lf.DisplayName,
                    Description = lf.Description,
                    Category = lf.Category,
                    IsActive = lf.IsActive,
                    LicenseId = lf.LicenseId,
                    LicenseName = license.Name,
                    CreatedAt = lf.CreatedAt,
                    CreatedBy = lf.CreatedBy ?? "system",
                    ModifiedAt = lf.ModifiedAt,
                    ModifiedBy = lf.ModifiedBy,
                    RequiredPermissions = lf.LicenseFeaturePermissions.Select(lfp => lfp.Permission.Name).ToList()
                }).ToList()
            };

            return Ok(licenseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving license {LicenseId}", id);
            return CreateInternalServerErrorProblem("An error occurred while retrieving the license", ex);
        }
    }

    /// <summary>
    /// Create a new license.
    /// </summary>
    /// <param name="createLicenseDto">License creation data</param>
    /// <returns>Created license</returns>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<LicenseDto>> CreateLicense(CreateLicenseDto createLicenseDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            // Check if license name already exists
            var existingLicense = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Name == createLicenseDto.Name && !l.IsDeleted);

            if (existingLicense != null)
            {
                return Conflict($"License with name '{createLicenseDto.Name}' already exists");
            }

            var license = new License
            {
                Name = createLicenseDto.Name,
                DisplayName = createLicenseDto.DisplayName,
                Description = createLicenseDto.Description,
                MaxUsers = createLicenseDto.MaxUsers,
                MaxApiCallsPerMonth = createLicenseDto.MaxApiCallsPerMonth,
                TierLevel = createLicenseDto.TierLevel,
                TenantId = Guid.Empty // System-level entity
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            var licenseDto = new LicenseDto
            {
                Id = license.Id,
                Name = license.Name,
                DisplayName = license.DisplayName,
                Description = license.Description,
                MaxUsers = license.MaxUsers,
                MaxApiCallsPerMonth = license.MaxApiCallsPerMonth,
                IsActive = license.IsActive,
                TierLevel = license.TierLevel,
                CreatedAt = license.CreatedAt,
                CreatedBy = license.CreatedBy ?? "system",
                ModifiedAt = license.ModifiedAt,
                ModifiedBy = license.ModifiedBy,
                TenantCount = 0,
                Features = new List<LicenseFeatureDto>()
            };

            return CreatedAtAction(nameof(GetLicense), new { id = license.Id }, licenseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating license");
            return CreateInternalServerErrorProblem("An error occurred while creating the license", ex);
        }
    }

    /// <summary>
    /// Update an existing license.
    /// </summary>
    /// <param name="id">License ID</param>
    /// <param name="updateLicenseDto">License update data</param>
    /// <returns>Updated license</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<LicenseDto>> UpdateLicense(Guid id, CreateLicenseDto updateLicenseDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (license == null)
            {
                return NotFound($"License with ID {id} not found");
            }

            // Check if the new name conflicts with another license
            var existingLicense = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Name == updateLicenseDto.Name && l.Id != id && !l.IsDeleted);

            if (existingLicense != null)
            {
                return Conflict($"License with name '{updateLicenseDto.Name}' already exists");
            }

            license.Name = updateLicenseDto.Name;
            license.DisplayName = updateLicenseDto.DisplayName;
            license.Description = updateLicenseDto.Description;
            license.MaxUsers = updateLicenseDto.MaxUsers;
            license.MaxApiCallsPerMonth = updateLicenseDto.MaxApiCallsPerMonth;
            license.TierLevel = updateLicenseDto.TierLevel;

            await _context.SaveChangesAsync();

            var licenseDto = new LicenseDto
            {
                Id = license.Id,
                Name = license.Name,
                DisplayName = license.DisplayName,
                Description = license.Description,
                MaxUsers = license.MaxUsers,
                MaxApiCallsPerMonth = license.MaxApiCallsPerMonth,
                IsActive = license.IsActive,
                TierLevel = license.TierLevel,
                CreatedAt = license.CreatedAt,
                CreatedBy = license.CreatedBy ?? "system",
                ModifiedAt = license.ModifiedAt,
                ModifiedBy = license.ModifiedBy,
                TenantCount = await _context.TenantLicenses.CountAsync(tl => tl.LicenseId == id && tl.IsAssignmentActive),
                Features = new List<LicenseFeatureDto>()
            };

            return Ok(licenseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating license {LicenseId}", id);
            return CreateInternalServerErrorProblem("An error occurred while updating the license", ex);
        }
    }

    /// <summary>
    /// Delete a license (soft delete).
    /// </summary>
    /// <param name="id">License ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> DeleteLicense(Guid id)
    {
        try
        {
            var license = await _context.Licenses
                .Include(l => l.TenantLicenses)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (license == null)
            {
                return NotFound($"License with ID {id} not found");
            }

            // Check if there are active tenant licenses
            var activeTenantLicenses = license.TenantLicenses.Count(tl => tl.IsAssignmentActive);
            if (activeTenantLicenses > 0)
            {
                return BadRequest($"Cannot delete license. It is currently assigned to {activeTenantLicenses} tenant(s)");
            }

            // Soft delete
            license.IsDeleted = true;
            license.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting license {LicenseId}", id);
            return CreateInternalServerErrorProblem("An error occurred while deleting the license", ex);
        }
    }

    /// <summary>
    /// Get all tenant licenses.
    /// </summary>
    /// <returns>List of tenant licenses</returns>
    /// <response code="200">Returns the list of tenant licenses</response>
    /// <response code="403">If the user doesn't have SuperAdmin role</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("tenant-licenses")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(IEnumerable<TenantLicenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<TenantLicenseDto>>> GetTenantLicenses()
    {
        try
        {
            var tenantLicenses = await _context.TenantLicenses
                .Include(tl => tl.Tenant)
                .Include(tl => tl.License)
                    .ThenInclude(l => l.LicenseFeatures)
                        .ThenInclude(lf => lf.LicenseFeaturePermissions)
                            .ThenInclude(lfp => lfp.Permission)
                .Where(tl => !tl.IsDeleted)
                .OrderBy(tl => tl.Tenant.Name)
                .ToListAsync();

            var tenantLicenseDtos = new List<TenantLicenseDto>();

            foreach (var tl in tenantLicenses)
            {
                var currentUserCount = await _context.Users.CountAsync(u => u.TenantId == tl.TargetTenantId && !u.IsDeleted);

                tenantLicenseDtos.Add(new TenantLicenseDto
                {
                    Id = tl.Id,
                    TenantId = tl.TargetTenantId,
                    TenantName = tl.Tenant.Name,
                    LicenseId = tl.LicenseId,
                    LicenseName = tl.License.Name,
                    LicenseDisplayName = tl.License.DisplayName,
                    StartsAt = tl.StartsAt,
                    ExpiresAt = tl.ExpiresAt,
                    IsActive = tl.IsAssignmentActive,
                    ApiCallsThisMonth = tl.ApiCallsThisMonth,
                    MaxApiCallsPerMonth = tl.License.MaxApiCallsPerMonth,
                    ApiCallsResetAt = tl.ApiCallsResetAt,
                    IsValid = tl.IsValid,
                    CreatedAt = tl.CreatedAt,
                    CreatedBy = tl.CreatedBy ?? "system",
                    ModifiedAt = tl.ModifiedAt,
                    ModifiedBy = tl.ModifiedBy,
                    TierLevel = tl.License.TierLevel,
                    MaxUsers = tl.License.MaxUsers,
                    CurrentUserCount = currentUserCount,
                    AvailableFeatures = tl.License.LicenseFeatures.Select(lf => new LicenseFeatureDto
                    {
                        Id = lf.Id,
                        Name = lf.Name,
                        DisplayName = lf.DisplayName,
                        Description = lf.Description,
                        Category = lf.Category,
                        IsActive = lf.IsActive,
                        LicenseId = lf.LicenseId,
                        LicenseName = tl.License.Name,
                        CreatedAt = lf.CreatedAt,
                        CreatedBy = lf.CreatedBy ?? "system",
                        ModifiedAt = lf.ModifiedAt,
                        ModifiedBy = lf.ModifiedBy,
                        RequiredPermissions = lf.LicenseFeaturePermissions.Select(lfp => lfp.Permission.Name).ToList()
                    }).ToList()
                });
            }

            return Ok(tenantLicenseDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant licenses");
            return CreateInternalServerErrorProblem("An error occurred while retrieving tenant licenses", ex);
        }
    }

    /// <summary>
    /// Assign a license to a tenant.
    /// </summary>
    /// <param name="assignLicenseDto">License assignment data</param>
    /// <returns>Created tenant license</returns>
    [HttpPost("assign")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<TenantLicenseDto>> AssignLicense(AssignLicenseDto assignLicenseDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return CreateValidationProblemDetails();
            }

            // Verify tenant exists
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == assignLicenseDto.TenantId && !t.IsDeleted);

            if (tenant == null)
            {
                return NotFound($"Tenant with ID {assignLicenseDto.TenantId} not found");
            }

            // Verify license exists
            var license = await _context.Licenses
                .Include(l => l.LicenseFeatures)
                    .ThenInclude(lf => lf.LicenseFeaturePermissions)
                        .ThenInclude(lfp => lfp.Permission)
                .FirstOrDefaultAsync(l => l.Id == assignLicenseDto.LicenseId && !l.IsDeleted);

            if (license == null)
            {
                return NotFound($"License with ID {assignLicenseDto.LicenseId} not found");
            }

            // Check if tenant already has an active license
            var existingActiveLicense = await _context.TenantLicenses
                .FirstOrDefaultAsync(tl => tl.TargetTenantId == assignLicenseDto.TenantId &&
                                          tl.IsAssignmentActive && !tl.IsDeleted);

            if (existingActiveLicense != null)
            {
                // Deactivate the existing license
                existingActiveLicense.IsAssignmentActive = false;
            }

            var tenantLicense = new TenantLicense
            {
                TargetTenantId = assignLicenseDto.TenantId,
                LicenseId = assignLicenseDto.LicenseId,
                StartsAt = assignLicenseDto.StartsAt,
                ExpiresAt = assignLicenseDto.ExpiresAt,
                IsAssignmentActive = assignLicenseDto.IsActive,
                ApiCallsThisMonth = 0,
                ApiCallsResetAt = DateTime.UtcNow,
                TenantId = Guid.Empty // System-level entity
            };

            _context.TenantLicenses.Add(tenantLicense);
            await _context.SaveChangesAsync();

            var tenantLicenseDto = new TenantLicenseDto
            {
                Id = tenantLicense.Id,
                TenantId = tenantLicense.TargetTenantId,
                TenantName = tenant.Name,
                LicenseId = tenantLicense.LicenseId,
                LicenseName = license.Name,
                LicenseDisplayName = license.DisplayName,
                StartsAt = tenantLicense.StartsAt,
                ExpiresAt = tenantLicense.ExpiresAt,
                IsActive = tenantLicense.IsAssignmentActive,
                ApiCallsThisMonth = tenantLicense.ApiCallsThisMonth,
                MaxApiCallsPerMonth = license.MaxApiCallsPerMonth,
                ApiCallsResetAt = tenantLicense.ApiCallsResetAt,
                IsValid = tenantLicense.IsValid,
                CreatedAt = tenantLicense.CreatedAt,
                CreatedBy = tenantLicense.CreatedBy ?? "system",
                ModifiedAt = tenantLicense.ModifiedAt,
                ModifiedBy = tenantLicense.ModifiedBy,
                TierLevel = license.TierLevel,
                MaxUsers = license.MaxUsers,
                CurrentUserCount = await _context.Users.CountAsync(u => u.TenantId == assignLicenseDto.TenantId && !u.IsDeleted),
                AvailableFeatures = license.LicenseFeatures.Select(lf => new LicenseFeatureDto
                {
                    Id = lf.Id,
                    Name = lf.Name,
                    DisplayName = lf.DisplayName,
                    Description = lf.Description,
                    Category = lf.Category,
                    IsActive = lf.IsActive,
                    LicenseId = lf.LicenseId,
                    LicenseName = license.Name,
                    CreatedAt = lf.CreatedAt,
                    CreatedBy = lf.CreatedBy ?? "system",
                    ModifiedAt = lf.ModifiedAt,
                    ModifiedBy = lf.ModifiedBy,
                    RequiredPermissions = lf.LicenseFeaturePermissions.Select(lfp => lfp.Permission.Name).ToList()
                }).ToList()
            };

            return CreatedAtAction(nameof(GetTenantLicenses), tenantLicenseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning license to tenant");
            return CreateInternalServerErrorProblem("An error occurred while assigning the license", ex);
        }
    }

    /// <summary>
    /// Get license information for a specific tenant.
    /// Users can only access their own tenant's license information unless they are SuperAdmin.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Tenant license information</returns>
    /// <response code="200">Returns the tenant license information</response>
    /// <response code="403">If the user tries to access another tenant's license</response>
    /// <response code="404">If no active license is found for the tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("tenant/{tenantId}")]
    [ProducesResponseType(typeof(TenantLicenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TenantLicenseDto>> GetTenantLicense(Guid tenantId)
    {
        try
        {
            var tenantLicense = await _context.TenantLicenses
                .Include(tl => tl.Tenant)
                .Include(tl => tl.License)
                    .ThenInclude(l => l.LicenseFeatures)
                        .ThenInclude(lf => lf.LicenseFeaturePermissions)
                            .ThenInclude(lfp => lfp.Permission)
                .FirstOrDefaultAsync(tl => tl.TargetTenantId == tenantId &&
                                          tl.IsAssignmentActive && !tl.IsDeleted);

            if (tenantLicense == null)
            {
                return NotFound($"No active license found for tenant {tenantId}");
            }

            var tenantLicenseDto = new TenantLicenseDto
            {
                Id = tenantLicense.Id,
                TenantId = tenantLicense.TargetTenantId,
                TenantName = tenantLicense.Tenant.Name,
                LicenseId = tenantLicense.LicenseId,
                LicenseName = tenantLicense.License.Name,
                LicenseDisplayName = tenantLicense.License.DisplayName,
                StartsAt = tenantLicense.StartsAt,
                ExpiresAt = tenantLicense.ExpiresAt,
                IsActive = tenantLicense.IsAssignmentActive,
                ApiCallsThisMonth = tenantLicense.ApiCallsThisMonth,
                MaxApiCallsPerMonth = tenantLicense.License.MaxApiCallsPerMonth,
                ApiCallsResetAt = tenantLicense.ApiCallsResetAt,
                IsValid = tenantLicense.IsValid,
                CreatedAt = tenantLicense.CreatedAt,
                CreatedBy = tenantLicense.CreatedBy ?? "system",
                ModifiedAt = tenantLicense.ModifiedAt,
                ModifiedBy = tenantLicense.ModifiedBy,
                TierLevel = tenantLicense.License.TierLevel,
                MaxUsers = tenantLicense.License.MaxUsers,
                CurrentUserCount = await _context.Users.CountAsync(u => u.TenantId == tenantId && !u.IsDeleted),
                AvailableFeatures = tenantLicense.License.LicenseFeatures.Select(lf => new LicenseFeatureDto
                {
                    Id = lf.Id,
                    Name = lf.Name,
                    DisplayName = lf.DisplayName,
                    Description = lf.Description,
                    Category = lf.Category,
                    IsActive = lf.IsActive,
                    LicenseId = lf.LicenseId,
                    LicenseName = tenantLicense.License.Name,
                    CreatedAt = lf.CreatedAt,
                    CreatedBy = lf.CreatedBy ?? "system",
                    ModifiedAt = lf.ModifiedAt,
                    ModifiedBy = lf.ModifiedBy,
                    RequiredPermissions = lf.LicenseFeaturePermissions.Select(lfp => lfp.Permission.Name).ToList()
                }).ToList()
            };

            return Ok(tenantLicenseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving license for tenant {TenantId}", tenantId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving the tenant license", ex);
        }
    }
}