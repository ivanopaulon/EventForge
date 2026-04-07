using EventForge.DTOs.Chat;

namespace EventForge.Server.Services.Interfaces;

/// <summary>
/// Interface for chat service operations.
/// This interface defines the contract for chat management operations
/// that will be implemented in Step 3 of the roadmap.
/// </summary>
public interface IChatService
{
    Task<ChatResponseDto> CreateChatAsync(CreateChatDto createChatDto);
    Task<ChatMessageDto> SendMessageAsync(SendMessageDto messageDto);
    Task<ChatMessageDto> EditMessageAsync(EditMessageDto editDto);
    Task<bool> DeleteMessageAsync(Guid messageId, Guid userId, string? reason = null);
    Task<bool> UserHasAccessToChatAsync(Guid userId, Guid chatId, Guid tenantId);
    Task<IEnumerable<Guid>> GetUserActiveChatIdsAsync(Guid userId, Guid tenantId);
    Task UpdateUserLastSeenAsync(Guid userId);
    Task<bool> MarkMessageAsReadAsync(Guid messageId, Guid userId);
    Task<bool> UpdateChatMembersAsync(UpdateChatMembersDto updateMembersDto);
    Task<bool> ModerateChatAsync(ChatModerationActionDto moderationAction);
    Task<ChatStatsDto> GetChatStatsAsync(Guid? tenantId = null);
    Task<IEnumerable<ChatResponseDto>> SearchChatsAsync(ChatSearchDto searchDto);
    Task<IEnumerable<ChatMessageDto>> SearchMessagesAsync(MessageSearchDto searchDto);
}