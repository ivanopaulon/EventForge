using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Store;


namespace EventForge.Server.Controllers;

public partial class StoreUsersController
{

    /// <summary>
    /// Uploads a photo for a store user (with GDPR consent validation).
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="file">Photo file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user with photo</returns>
    /// <response code="200">Photo uploaded successfully</response>
    /// <response code="400">If file is invalid or consent not given</response>
    /// <response code="404">If store user not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("{id:guid}/photo")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(StoreUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StoreUserDto>> UploadStoreUserPhoto(
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
            var updatedStoreUser = await storeUserService.UploadStoreUserPhotoAsync(id, file, cancellationToken);
            if (updatedStoreUser is null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found.");
            }

            return Ok(updatedStoreUser);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("consent"))
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the photo.", ex);
        }
    }

    /// <summary>
    /// Gets the photo DocumentReference for a store user.
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Photo document reference</returns>
    /// <response code="200">Returns the photo document</response>
    /// <response code="404">If store user not found or has no photo</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{id:guid}/photo")]
    [ProducesResponseType(typeof(Prym.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStoreUserPhotoDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var photoDocument = await storeUserService.GetStoreUserPhotoDocumentAsync(id, cancellationToken);
            if (photoDocument is null)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found or has no photo.");
            }

            return Ok(photoDocument);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the photo.", ex);
        }
    }

    /// <summary>
    /// Deletes the photo for a store user.
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Photo deleted successfully</response>
    /// <response code="404">If store user not found or has no photo</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("{id:guid}/photo")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStoreUserPhoto(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await storeUserService.DeleteStoreUserPhotoAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store user with ID {id} not found or has no photo.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the photo.", ex);
        }
    }

}
