using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Store;


namespace EventForge.Server.Controllers;

public partial class StoreUsersController
{

    /// <summary>
    /// Uploads an image for a store POS.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="file">Image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store POS with image</returns>
    /// <response code="200">Image uploaded successfully</response>
    /// <response code="400">If file is invalid</response>
    /// <response code="404">If store POS not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("pos/{id:guid}/image")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StorePosDto>> UploadStorePosImage(
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
            var updatedStorePos = await storeUserService.UploadStorePosImageAsync(id, file, cancellationToken);
            if (updatedStorePos is null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return Ok(updatedStorePos);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading the image.", ex);
        }
    }

    /// <summary>
    /// Gets the image DocumentReference for a store POS.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image document reference</returns>
    /// <response code="200">Returns the image document</response>
    /// <response code="404">If store POS not found or has no image</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("pos/{id:guid}/image")]
    [ProducesResponseType(typeof(Prym.DTOs.Teams.DocumentReferenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetStorePosImageDocument(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var imageDocument = await storeUserService.GetStorePosImageDocumentAsync(id, cancellationToken);
            if (imageDocument is null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found or has no image.");
            }

            return Ok(imageDocument);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the image.", ex);
        }
    }

    /// <summary>
    /// Deletes the image for a store POS.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Image deleted successfully</response>
    /// <response code="404">If store POS not found or has no image</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("pos/{id:guid}/image")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteStorePosImage(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var deleted = await storeUserService.DeleteStorePosImageAsync(id, cancellationToken);
            if (!deleted)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found or has no image.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the image.", ex);
        }
    }

}
