using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Chat;
using System.Diagnostics;


namespace EventForge.Server.Services.Chat;

public partial class ChatService
{

    /// <summary>
    /// Localizes chat content based on user preferences.
    /// </summary>
    public async Task<ChatMessageDto> LocalizeChatMessageAsync(
        ChatMessageDto message,
        string targetLocale,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {

        // 1. Check if message.Locale == targetLocale (already localized)
        if (message.Locale == targetLocale)
        {
            logger.LogDebug("Message {MessageId} is already in locale {Locale}", message.Id, targetLocale);
            return message;
        }

        // 2. Update message.Locale = targetLocale
        message.Locale = targetLocale;

        // 3. Store localized content in metadata
        // For Phase 1: Simple implementation - just update Locale field
        // Future: Integrate with translation service for actual content translation
        if (message.Metadata is null)
        {
            message.Metadata = new Dictionary<string, object>();
        }

        message.Metadata["LocalizedTo"] = targetLocale;
        message.Metadata["LocalizedAt"] = DateTime.UtcNow.ToString("O");
        if (userId.HasValue)
        {
            message.Metadata["LocalizedBy"] = userId.Value.ToString();
        }

        // Note: Localization is transient (client-side only) and not persisted to database
        await Task.CompletedTask; // Satisfy async signature

        // 4. Return localized ChatMessageDto
        return message;
    }

    /// <summary>
    /// Updates chat localization preferences for users.
    /// </summary>
    public async Task<ChatLocalizationPreferencesDto> UpdateChatLocalizationAsync(
        Guid userId,
        ChatLocalizationPreferencesDto preferences,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Find user
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && (tenantId == null || u.TenantId == tenantId.Value), cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }

        // 2. Store preferences in User's PreferredLanguage field
        // For Phase 1: Simple implementation using existing User fields
        var oldLocale = user.PreferredLanguage;
        user.PreferredLanguage = preferences.PreferredLocale;
        user.ModifiedAt = DateTime.UtcNow;

        // 3. Save to database
        await context.SaveChangesAsync(cancellationToken);

        // 4. Log audit trail
        _ = await auditLogService.LogEntityChangeAsync(
            entityName: "ChatLocalizationPreferences",
            entityId: userId,
            propertyName: "PreferredLocale",
            operationType: "Update",
            oldValue: oldLocale,
            newValue: preferences.PreferredLocale,
            changedBy: userId.ToString(),
            entityDisplayName: $"Chat Localization: {userId}",
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Updated chat localization preferences for user {UserId}: Locale={Locale}, AutoTranslate={AutoTranslate}",
            userId, preferences.PreferredLocale, preferences.AutoTranslate);

        // 5. Return updated preferences
        preferences.UserId = userId;
        return preferences;
    }

}
