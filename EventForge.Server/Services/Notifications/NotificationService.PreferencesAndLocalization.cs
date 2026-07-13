using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Notifications;
using System.Diagnostics;


namespace EventForge.Server.Services.Notifications;

public partial class NotificationService
{

    /// <summary>
    /// Gets user notification preferences with tenant defaults.
    /// Queries User.MetadataJson for stored preferences or returns defaults.
    /// </summary>
    public async Task<NotificationPreferencesDto> GetUserPreferencesAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {

        try
        {
            // 1. Query User entity by userId and tenantId
            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);

            // 2. If user found and has preferences in metadata
            if (user?.MetadataJson is not null)
            {
                try
                {
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(user.MetadataJson);

                    if (metadata?.ContainsKey("NotificationPreferences") == true)
                    {
                        // Deserialize the JsonElement directly to NotificationPreferencesDto
                        var preferences = System.Text.Json.JsonSerializer.Deserialize<NotificationPreferencesDto>(
                            metadata["NotificationPreferences"].GetRawText());

                        if (preferences is not null)
                        {
                            preferences.UserId = userId;
                            preferences.TenantId = tenantId;
                            logger.LogDebug("Retrieved stored notification preferences for user {UserId}", userId);
                            return preferences;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse notification preferences from user metadata for user {UserId}", userId);
                    // Continue to return defaults
                }
            }

            // 3. Return default preferences if not found or parsing failed
            logger.LogDebug("Returning default notification preferences for user {UserId}", userId);
            return new NotificationPreferencesDto
            {
                UserId = userId,
                TenantId = tenantId,
                NotificationsEnabled = true,
                MinPriority = NotificationPriority.Low,
                EnabledTypes = Enum.GetValues<NotificationTypes>().ToList(),
                PreferredLocale = "en-US",
                SoundEnabled = true,
                AutoArchiveAfterDays = 30
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve notification preferences for user {UserId}", userId);

            // Return defaults on error
            return new NotificationPreferencesDto
            {
                UserId = userId,
                TenantId = tenantId,
                NotificationsEnabled = true,
                MinPriority = NotificationPriority.Low,
                EnabledTypes = Enum.GetValues<NotificationTypes>().ToList(),
                PreferredLocale = "en-US",
                SoundEnabled = true,
                AutoArchiveAfterDays = 30
            };
        }
    }

    /// <summary>
    /// Updates user notification preferences with validation and audit trail.
    /// Persists preferences to User.MetadataJson.
    /// </summary>
    public async Task<NotificationPreferencesDto> UpdateUserPreferencesAsync(
        NotificationPreferencesDto preferences,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Find user by preferences.UserId and TenantId
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == preferences.UserId && u.TenantId == preferences.TenantId, cancellationToken);

            if (user is null)
            {
                throw new InvalidOperationException($"User {preferences.UserId} not found in tenant {preferences.TenantId}");
            }

            // 2. Get current preferences for audit trail
            var oldPreferences = await GetUserPreferencesAsync(preferences.UserId, preferences.TenantId, cancellationToken);

            // 3. Update User.MetadataJson with new preferences
            Dictionary<string, System.Text.Json.JsonElement> metadata;

            if (user.MetadataJson is not null)
            {
                try
                {
                    var existingMetadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(user.MetadataJson);
                    metadata = existingMetadata ?? new Dictionary<string, System.Text.Json.JsonElement>();
                }
                catch (System.Text.Json.JsonException)
                {
                    metadata = new Dictionary<string, System.Text.Json.JsonElement>();
                }
            }
            else
            {
                metadata = new Dictionary<string, System.Text.Json.JsonElement>();
            }

            // Serialize preferences DTO to JSON and convert to JsonElement
            var preferencesJson = System.Text.Json.JsonSerializer.Serialize(preferences);
            var preferencesElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(preferencesJson);

            metadata["NotificationPreferences"] = preferencesElement;
            user.MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            user.ModifiedAt = DateTime.UtcNow;

            // 4. Save to database
            await context.SaveChangesAsync(cancellationToken);

            // 5. Log audit trail with old vs new values
            await auditLogService.LogEntityChangeAsync(
                entityName: "NotificationPreferences",
                entityId: preferences.UserId,
                propertyName: "Preferences",
                operationType: "Update",
                oldValue: System.Text.Json.JsonSerializer.Serialize(oldPreferences),
                newValue: System.Text.Json.JsonSerializer.Serialize(preferences),
                changedBy: preferences.UserId.ToString(),
                entityDisplayName: $"Notification Preferences: {preferences.UserId}",
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Updated notification preferences for user {UserId}: Locale={Locale}, Enabled={Enabled}, MinPriority={MinPriority}",
                preferences.UserId, preferences.PreferredLocale, preferences.NotificationsEnabled, preferences.MinPriority);

            // 6. Return updated preferences
            return preferences;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update notification preferences for user {UserId}", preferences.UserId);
            throw new InvalidOperationException("Failed to update notification preferences", ex);
        }
    }

    /// <summary>
    /// Localizes notification content based on user preferences.
    /// Currently updates locale field with placeholder for future translation service integration.
    /// </summary>
    public async Task<NotificationResponseDto> LocalizeNotificationAsync(
        NotificationResponseDto notification,
        string targetLocale,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Check if already in target locale (early return)
        if (notification.Payload.Locale == targetLocale)
        {
            logger.LogDebug("Notification {NotificationId} already in target locale {Locale}",
                notification.Id, targetLocale);
            return notification;
        }

        logger.LogInformation(
            "Localizing notification {NotificationId} to locale {Locale} for user {UserId}",
            notification.Id, targetLocale, userId);

        // 2. Update notification.Payload.Locale
        notification.Payload.Locale = targetLocale;

        // Translation of payload content requires ITranslationService (future integration).

        // 4. Log localization request for analytics
        logger.LogDebug(
            "Localized notification {NotificationId} to {ToLocale} (translation service integration pending)",
            notification.Id, targetLocale);

        // Suppress async warning for future-proofing
        await Task.CompletedTask;

        // 5. Return localized notification
        return notification;
    }

}
