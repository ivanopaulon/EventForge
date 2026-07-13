using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Store;


namespace EventForge.Server.Controllers;

public partial class StoreUsersController
{

    /// <summary>
    /// Uploads a logo for a store user group.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="file">Logo file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user group with logo</returns>
    /// <response code="200">Logo uploaded successfully</response>
    /// <response code="400">If file is invalid</response>
    /// <response code="404">If store user group not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("groups/{id:guid}/logo")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreUserGroupDto>> UploadStoreUserGroupLogo(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (file == null || file.Length == 0)
        {
            return CreateValidationProblemDetails("File cannot be empty");
        }

        // Check file size limit (5MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return CreateValidationProblemDetails($"File size cannot exceed {maxFileSize / (1024 * 1024)} MB");
        }

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
        {
            return CreateValidationProblemDetails("Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");
        }

        try
        {
            var updatedGroup = await storeUserService.UploadStoreUserGroupLogoAsync(id, file, cancellationToken);
            if (updatedGroup is null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found.");
            }

            return Ok(updatedGroup);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the logo.", ex);
        }
    }

    /// <summary>
    /// Gets the logo DocumentReference for a store user group.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logo document reference</returns>
    /// <response code="200">Returns the logo document</response>
    /// <response code="404">If store user group not found or has no logo</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("groups/{id:guid}/logo")]
    [ProducesResponseType(typeof(Prym.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStoreUserGroupLogoDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var logoDocument = await storeUserService.GetStoreUserGroupLogoDocumentAsync(id, cancellationToken);
            if (logoDocument is null)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found or has no logo.");
            }

            return Ok(logoDocument);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the logo.", ex);
        }
    }

    /// <summary>
    /// Deletes the logo for a store user group.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Logo deleted successfully</response>
    /// <response code="404">If store user group not found or has no logo</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("groups/{id:guid}/logo")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStoreUserGroupLogo(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await storeUserService.DeleteStoreUserGroupLogoAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user group with ID {id} not found or has no logo.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the logo.", ex);
        }
    }

}
