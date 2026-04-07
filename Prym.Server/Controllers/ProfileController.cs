using Prym.DTOs.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Controllers;

/// <summary>
/// REST API controller for user profile management.
/// Provides operations for managing user profile information, avatar, password, notifications, and sessions.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class ProfileController(
    PrymDbContext context,
    ITenantContext tenantContext,
    IPasswordService passwordService,
    ILogger<ProfileController> logger) : BaseApiController
{

    /// <summary>
    /// Gets current user's profile information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile information</returns>
    /// <response code="200">Returns the user profile</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If user profile is not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user is null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            var profileDto = MapToProfileDto(user);

            // Load DisplayPreferences from MetadataJson
            profileDto.DisplayPreferences = LoadDisplayPreferencesFromMetadata(user);

            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving your profile.", ex);
        }
    }

    /// <summary>
    /// Updates current user's profile information.
    /// </summary>
    /// <param name="updateDto">Updated profile information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile</returns>
    /// <response code="200">Returns the updated user profile</response>
    /// <response code="400">If the input is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile(
        [FromBody] UpdateProfileDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user is null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            // Update profile fields
            user.FirstName = updateDto.FirstName;
            user.LastName = updateDto.LastName;
            user.Email = updateDto.Email;
            user.PhoneNumber = updateDto.PhoneNumber;
            user.PreferredLanguage = updateDto.PreferredLanguage;
            user.TimeZone = updateDto.TimeZone;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            // Salva DisplayPreferences in MetadataJson
            if (updateDto.DisplayPreferences is not null)
            {
                Dictionary<string, object> metadata;

                if (string.IsNullOrEmpty(user.MetadataJson))
                {
                    metadata = new Dictionary<string, object>();
                }
                else
                {
                    try
                    {
                        metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(user.MetadataJson)
                                   ?? new Dictionary<string, object>();
                    }
                    catch
                    {
                        metadata = new Dictionary<string, object>();
                    }
                }

                metadata["DisplayPreferences"] = updateDto.DisplayPreferences;
                user.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);

                logger.LogInformation("Updated DisplayPreferences for user {UserId}", userId);
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} updated their profile", userId.Value);

            var profileDto = MapToProfileDto(user);

            // Load DisplayPreferences from MetadataJson for the return value
            profileDto.DisplayPreferences = LoadDisplayPreferencesFromMetadata(user);

            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating your profile.", ex);
        }
    }

    /// <summary>
    /// Uploads user avatar.
    /// </summary>
    /// <param name="file">Avatar image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile with avatar URL</returns>
    /// <response code="200">Returns the updated user profile with avatar</response>
    /// <response code="400">If the file is invalid</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError("file", "File is required.");
            return CreateValidationProblemDetails();
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            ModelState.AddModelError("file", "Only image files (jpg, jpeg, png, gif) are allowed.");
            return CreateValidationProblemDetails();
        }

        // Validate file size (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError("file", "File size cannot exceed 5MB.");
            return CreateValidationProblemDetails();
        }

        try
        {
            var userId = tenantContext.CurrentUserId;
            var tenantId = tenantContext.CurrentTenantId;

            if (!userId.HasValue || !tenantId.HasValue)
            {
                return Unauthorized();
            }

            var user = await context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user is null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            // Generate a unique filename
            var fileName = $"user_{userId.Value}_{Guid.NewGuid()}{extension}";

            // Save to wwwroot/images/avatars
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.GetFullPath(Path.Combine(uploadsFolder, fileName));

            // Validate that the file path is within the allowed directory (prevent path traversal)
            var uploadsFullPath = Path.GetFullPath(uploadsFolder);
            if (!filePath.StartsWith(uploadsFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid file path detected.");
            }

            var storageKey = $"/images/avatars/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Create or update DocumentReference
            var documentReference = new Prym.Server.Data.Entities.Teams.DocumentReference
            {
                TenantId = tenantId.Value,
                OwnerId = userId.Value,
                OwnerType = "User",
                FileName = file.FileName,
                Type = Prym.DTOs.Common.DocumentReferenceType.ProfilePhoto,
                SubType = Prym.DTOs.Common.DocumentReferenceSubType.None,
                MimeType = file.ContentType,
                StorageKey = storageKey,
                Url = storageKey,
                FileSizeBytes = file.Length,
                Title = $"User {user.FullName} Avatar",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.Username
            };

            // If user already has an avatar, delete the old one first
            if (user.AvatarDocumentId.HasValue)
            {
                var oldDocument = await context.DocumentReferences
                    .FirstOrDefaultAsync(d => d.Id == user.AvatarDocumentId.Value, cancellationToken);

                if (oldDocument is not null)
                {
                    // Delete old physical file with path validation
                    var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var oldFilePath = Path.GetFullPath(Path.Combine(wwwrootPath, oldDocument.StorageKey.TrimStart('/')));

                    // Validate that the file path is within wwwroot (prevent path traversal)
                    var wwwrootFullPath = Path.GetFullPath(wwwrootPath);
                    if (oldFilePath.StartsWith(wwwrootFullPath, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    context.DocumentReferences.Remove(oldDocument);
                }
            }

            context.DocumentReferences.Add(documentReference);
            await context.SaveChangesAsync(cancellationToken);

            // Update user with new DocumentReference ID
            user.AvatarDocumentId = documentReference.Id;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} uploaded avatar successfully as DocumentReference {DocumentId}", userId.Value, documentReference.Id);

            // Reload to get the document reference
            user.AvatarDocument = documentReference;
            var profileDto = MapToProfileDto(user);
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while uploading your avatar.", ex);
        }
    }

    /// <summary>
    /// Deletes user avatar.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Avatar deleted successfully</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If user or avatar is not found</response>
    [HttpDelete("avatar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await context.Users
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user is null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            if (!user.AvatarDocumentId.HasValue || user.AvatarDocument is null)
            {
                return CreateNotFoundProblem("Avatar not found.");
            }

            // Delete physical file with path validation
            var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.GetFullPath(Path.Combine(wwwrootPath, user.AvatarDocument.StorageKey.TrimStart('/')));

            // Validate that the file path is within wwwroot (prevent path traversal)
            var wwwrootFullPath = Path.GetFullPath(wwwrootPath);
            if (filePath.StartsWith(wwwrootFullPath, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Remove DocumentReference
            context.DocumentReferences.Remove(user.AvatarDocument);

            // Update user
            user.AvatarDocumentId = null;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} deleted their avatar", userId.Value);

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting your avatar.", ex);
        }
    }

    /// <summary>
    /// Changes user password.
    /// </summary>
    /// <param name="changePasswordDto">Password change request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Password changed successfully</response>
    /// <response code="400">If the input is invalid or current password is incorrect</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto changePasswordDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user is null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            // Verify current password
            if (!passwordService.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return CreateValidationProblemDetails();
            }

            // Validate new password
            var validationResult = passwordService.ValidatePassword(changePasswordDto.NewPassword);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError("NewPassword", error);
                }
                return CreateValidationProblemDetails();
            }

            // Hash new password
            var (hash, salt) = passwordService.HashPassword(changePasswordDto.NewPassword);

            // Update user
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} changed their password", userId.Value);

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while changing your password.", ex);
        }
    }

    /// <summary>
    /// Updates user notification preferences.
    /// </summary>
    /// <param name="preferencesDto">Notification preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user profile</returns>
    /// <response code="200">Returns the updated user profile</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpPut("notifications")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesDto preferencesDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user is null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            // Update notification preferences
            user.EmailNotificationsEnabled = preferencesDto.EmailNotificationsEnabled;
            user.PushNotificationsEnabled = preferencesDto.PushNotificationsEnabled;
            user.InAppNotificationsEnabled = preferencesDto.InAppNotificationsEnabled;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} updated notification preferences", userId.Value);

            var profileDto = MapToProfileDto(user);
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating notification preferences.", ex);
        }
    }

    /// <summary>
    /// Gets active sessions for current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sessions</returns>
    /// <response code="200">Returns the list of active sessions</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<ActiveSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ActiveSessionDto>>> GetActiveSessions(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Get sessions from LoginAudit where EventType = "Success" and no logout event
            var sessions = await context.LoginAudits
                .Where(la => la.UserId == userId.Value &&
                            la.EventType == "Success" &&
                            la.Success &&
                            la.SessionId != null)
                .OrderByDescending(la => la.EventTime)
                .Take(10) // Get last 10 sessions
                .Select(la => new ActiveSessionDto
                {
                    Id = la.Id,
                    IpAddress = la.IpAddress,
                    UserAgent = la.UserAgent,
                    Browser = ParseBrowser(la.UserAgent),
                    OperatingSystem = ParseOperatingSystem(la.UserAgent),
                    DeviceType = ParseDeviceType(la.UserAgent),
                    LoginTime = la.EventTime,
                    LastActivity = la.EventTime,
                    IsCurrentSession = false, // We'll determine this by comparing session IDs if available
                    Location = null // Could be implemented with IP geolocation service
                })
                .ToListAsync(cancellationToken);

            return Ok(sessions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving active sessions.", ex);
        }
    }

    /// <summary>
    /// Terminates a specific session.
    /// </summary>
    /// <param name="sessionId">Session ID to terminate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Session terminated successfully</response>
    /// <response code="401">If user is not authenticated</response>
    /// <response code="404">If session is not found</response>
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TerminateSession(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var session = await context.LoginAudits
                .FirstOrDefaultAsync(la => la.Id == sessionId && la.UserId == userId.Value, cancellationToken);

            if (session is null)
            {
                return CreateNotFoundProblem("Session not found.");
            }

            // Create logout audit entry
            var logoutAudit = new LoginAudit
            {
                TenantId = session.TenantId,
                UserId = userId.Value,
                Username = session.Username,
                EventType = "Logout",
                IpAddress = session.IpAddress,
                UserAgent = session.UserAgent,
                EventTime = DateTime.UtcNow,
                Success = true,
                SessionId = session.SessionId,
                SessionDuration = DateTime.UtcNow - session.EventTime,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = session.Username
            };

            context.LoginAudits.Add(logoutAudit);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} terminated session {SessionId}", userId.Value, sessionId);

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while terminating the session.", ex);
        }
    }

    /// <summary>
    /// Terminates all other sessions except current.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">All other sessions terminated successfully</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpDelete("sessions/all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TerminateAllOtherSessions(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Get all active sessions except current
            var sessions = await context.LoginAudits
                .Where(la => la.UserId == userId.Value &&
                            la.EventType == "Success" &&
                            la.Success &&
                            la.SessionId != null)
                .ToListAsync(cancellationToken);

            var username = sessions.FirstOrDefault()?.Username ?? "Unknown";

            // Create logout entries for all sessions
            foreach (var session in sessions)
            {
                var logoutAudit = new LoginAudit
                {
                    TenantId = session.TenantId,
                    UserId = userId.Value,
                    Username = username,
                    EventType = "Logout",
                    IpAddress = session.IpAddress,
                    UserAgent = session.UserAgent,
                    EventTime = DateTime.UtcNow,
                    Success = true,
                    SessionId = session.SessionId,
                    SessionDuration = DateTime.UtcNow - session.EventTime,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = username
                };

                context.LoginAudits.Add(logoutAudit);
            }

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("User {UserId} terminated all other sessions ({Count} sessions)", userId.Value, sessions.Count);

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while terminating sessions.", ex);
        }
    }

    /// <summary>
    /// Gets login history for current user.
    /// </summary>
    /// <param name="days">Number of days to retrieve (default 30)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of login history entries</returns>
    /// <response code="200">Returns the login history</response>
    /// <response code="401">If user is not authenticated</response>
    [HttpGet("login-history")]
    [ProducesResponseType(typeof(List<LoginHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<LoginHistoryDto>>> GetLoginHistory(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-Math.Abs(days));

            var history = await context.LoginAudits
                .Where(la => la.UserId == userId.Value &&
                            la.EventTime >= cutoffDate &&
                            (la.EventType == "Success" || la.EventType == "Failed"))
                .OrderByDescending(la => la.EventTime)
                .Select(la => new LoginHistoryDto
                {
                    Id = la.Id,
                    LoginTime = la.EventTime,
                    IpAddress = la.IpAddress,
                    UserAgent = la.UserAgent,
                    Browser = ParseBrowser(la.UserAgent),
                    Location = null,
                    WasSuccessful = la.Success,
                    FailureReason = la.FailureReason
                })
                .ToListAsync(cancellationToken);

            return Ok(history);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving login history.", ex);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Loads DisplayPreferences from user metadata JSON.
    /// </summary>
    /// <param name="user">The user entity</param>
    /// <returns>UserDisplayPreferencesDto if found and valid, null otherwise</returns>
    private UserDisplayPreferencesDto? LoadDisplayPreferencesFromMetadata(User user)
    {
        if (string.IsNullOrEmpty(user.MetadataJson))
        {
            return null;
        }

        try
        {
            var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(user.MetadataJson);

            if (metadata?.ContainsKey("DisplayPreferences") == true)
            {
                var displayPrefs = System.Text.Json.JsonSerializer.Deserialize<UserDisplayPreferencesDto>(
                    metadata["DisplayPreferences"].GetRawText());

                if (displayPrefs is not null)
                {
                    // Backward compatibility: ensure defaults for new fields
                    if (string.IsNullOrEmpty(displayPrefs.HeadingsFont))
                        displayPrefs.HeadingsFont = "Noto Sans Display";

                    if (string.IsNullOrEmpty(displayPrefs.BodyFont))
                    {
                        // Migrate from PrimaryFontFamily if present
                        displayPrefs.BodyFont = !string.IsNullOrEmpty(displayPrefs.BodyFont)
                            ? displayPrefs.BodyFont
                            : "Noto Sans";
                    }

                    if (string.IsNullOrEmpty(displayPrefs.ContentFont))
                        displayPrefs.ContentFont = "Noto Serif";

                    if (string.IsNullOrEmpty(displayPrefs.MonospaceFont))
                    {
                        // Migrate from MonospaceFontFamily if present
                        displayPrefs.MonospaceFont = !string.IsNullOrEmpty(displayPrefs.MonospaceFont)
                            ? displayPrefs.MonospaceFont
                            : "Noto Sans Mono";
                    }

                    if (displayPrefs.BaseFontSize < 12 || displayPrefs.BaseFontSize > 24)
                        displayPrefs.BaseFontSize = 16;
                }

                return displayPrefs;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse DisplayPreferences from user metadata for user {UserId}", user.Id);
        }

        return null;
    }

    private UserProfileDto MapToProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarDocument?.Url,
            AvatarDocumentId = user.AvatarDocumentId,
            PreferredLanguage = user.PreferredLanguage,
            TimeZone = user.TimeZone,
            EmailNotificationsEnabled = user.EmailNotificationsEnabled,
            PushNotificationsEnabled = user.PushNotificationsEnabled,
            InAppNotificationsEnabled = user.InAppNotificationsEnabled,
            Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>(),
            TenantId = user.TenantId,
            TenantName = user.Tenant?.Name,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            PasswordChangedAt = user.PasswordChangedAt
        };
    }

    private static string? ParseBrowser(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;

        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox";
        if (userAgent.Contains("Safari")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        if (userAgent.Contains("Opera")) return "Opera";

        return "Unknown";
    }

    private static string? ParseOperatingSystem(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;

        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("Mac OS")) return "macOS";
        if (userAgent.Contains("Linux")) return "Linux";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("iOS")) return "iOS";

        return "Unknown";
    }

    private static string? ParseDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;

        if (userAgent.Contains("Mobile")) return "Mobile";
        if (userAgent.Contains("Tablet")) return "Tablet";

        return "Desktop";
    }

    #endregion
}
