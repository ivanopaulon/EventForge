using EventForge.DTOs.Profile;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Filters;
using EventForge.Server.Services.Auth;
using EventForge.Server.Services.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for user profile management.
/// Provides operations for managing user profile information, avatar, password, notifications, and sessions.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        IPasswordService passwordService,
        ILogger<ProfileController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            var profileDto = MapToProfileDto(user);
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving profile for user {UserId}", _tenantContext.CurrentUserId);
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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user == null)
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

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} updated their profile", userId.Value);

            var profileDto = MapToProfileDto(user);
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", _tenantContext.CurrentUserId);
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
        if (file == null || file.Length == 0)
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
            var userId = _tenantContext.CurrentUserId;
            var tenantId = _tenantContext.CurrentTenantId;
            
            if (!userId.HasValue || !tenantId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            // Generate a unique filename
            var fileName = $"user_{userId.Value}_{Guid.NewGuid()}{extension}";

            // Save to wwwroot/images/avatars
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatars");
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);
            var storageKey = $"/images/avatars/{fileName}";

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Create or update DocumentReference
            var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
            {
                TenantId = tenantId.Value,
                OwnerId = userId.Value,
                OwnerType = "User",
                FileName = file.FileName,
                Type = EventForge.DTOs.Common.DocumentReferenceType.ProfilePhoto,
                SubType = EventForge.DTOs.Common.DocumentReferenceSubType.None,
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
                var oldDocument = await _context.DocumentReferences
                    .FirstOrDefaultAsync(d => d.Id == user.AvatarDocumentId.Value, cancellationToken);

                if (oldDocument != null)
                {
                    // Delete old physical file
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldDocument.StorageKey.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    _context.DocumentReferences.Remove(oldDocument);
                }
            }

            _context.DocumentReferences.Add(documentReference);
            await _context.SaveChangesAsync(cancellationToken);

            // Update user with new DocumentReference ID
            user.AvatarDocumentId = documentReference.Id;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} uploaded avatar successfully as DocumentReference {DocumentId}", userId.Value, documentReference.Id);

            // Reload to get the document reference
            user.AvatarDocument = documentReference;
            var profileDto = MapToProfileDto(user);
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for user {UserId}", _tenantContext.CurrentUserId);
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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            if (!user.AvatarDocumentId.HasValue || user.AvatarDocument == null)
            {
                return CreateNotFoundProblem("Avatar not found.");
            }

            // Delete physical file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarDocument.StorageKey.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Remove DocumentReference
            _context.DocumentReferences.Remove(user.AvatarDocument);

            // Update user
            user.AvatarDocumentId = null;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} deleted their avatar", userId.Value);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting avatar for user {UserId}", _tenantContext.CurrentUserId);
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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                return CreateValidationProblemDetails();
            }

            // Validate new password
            var validationResult = _passwordService.ValidatePassword(changePasswordDto.NewPassword);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError("NewPassword", error);
                }
                return CreateValidationProblemDetails();
            }

            // Hash new password
            var (hash, salt) = _passwordService.HashPassword(changePasswordDto.NewPassword);

            // Update user
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.PasswordChangedAt = DateTime.UtcNow;
            user.MustChangePassword = false;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} changed their password", userId.Value);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", _tenantContext.CurrentUserId);
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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Tenant)
                .Include(u => u.AvatarDocument)
                .FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                return CreateNotFoundProblem("User profile not found.");
            }

            // Update notification preferences
            user.EmailNotificationsEnabled = preferencesDto.EmailNotificationsEnabled;
            user.PushNotificationsEnabled = preferencesDto.PushNotificationsEnabled;
            user.InAppNotificationsEnabled = preferencesDto.InAppNotificationsEnabled;
            user.ModifiedAt = DateTime.UtcNow;
            user.ModifiedBy = user.Username;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} updated notification preferences", userId.Value);

            var profileDto = MapToProfileDto(user);
            return Ok(profileDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preferences for user {UserId}", _tenantContext.CurrentUserId);
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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Get sessions from LoginAudit where EventType = "Success" and no logout event
            var sessions = await _context.LoginAudits
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
            _logger.LogError(ex, "Error retrieving active sessions for user {UserId}", _tenantContext.CurrentUserId);
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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var session = await _context.LoginAudits
                .FirstOrDefaultAsync(la => la.Id == sessionId && la.UserId == userId.Value, cancellationToken);

            if (session == null)
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

            _context.LoginAudits.Add(logoutAudit);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} terminated session {SessionId}", userId.Value, sessionId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating session {SessionId} for user {UserId}", sessionId, _tenantContext.CurrentUserId);
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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            // Get all active sessions except current
            var sessions = await _context.LoginAudits
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

                _context.LoginAudits.Add(logoutAudit);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} terminated all other sessions ({Count} sessions)", userId.Value, sessions.Count);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating all sessions for user {UserId}", _tenantContext.CurrentUserId);
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
            var userId = _tenantContext.CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var cutoffDate = DateTime.UtcNow.AddDays(-Math.Abs(days));

            var history = await _context.LoginAudits
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
            _logger.LogError(ex, "Error retrieving login history for user {UserId}", _tenantContext.CurrentUserId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving login history.", ex);
        }
    }

    #region Private Helper Methods

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
