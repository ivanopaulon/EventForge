using EventForge.Server.Data.Entities.Chat;
using EventForge.Server.Data.Entities.Notifications;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    private static void ConfigureChatRelationships(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<NotificationRecipient>()
            .HasOne(nr => nr.Notification)
            .WithMany(n => n.Recipients)
            .HasForeignKey(nr => nr.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<NotificationRecipient>()
            .HasIndex(nr => nr.UserId)
            .HasDatabaseName("IX_NotificationRecipients_UserId");

        _ = modelBuilder.Entity<ChatMember>()
            .HasOne(cm => cm.ChatThread)
            .WithMany(ct => ct.Members)
            .HasForeignKey(cm => cm.ChatThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<ChatMember>()
            .HasIndex(cm => cm.UserId)
            .HasDatabaseName("IX_ChatMembers_UserId");

        _ = modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.ChatThread)
            .WithMany(ct => ct.Messages)
            .HasForeignKey(cm => cm.ChatThreadId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<ChatMessage>()
            .HasIndex(cm => cm.SenderId)
            .HasDatabaseName("IX_ChatMessages_SenderId");

        _ = modelBuilder.Entity<ChatMessage>()
            .HasOne(cm => cm.ReplyToMessage)
            .WithMany(m => m.Replies)
            .HasForeignKey(cm => cm.ReplyToMessageId)
            .OnDelete(DeleteBehavior.NoAction);

        _ = modelBuilder.Entity<MessageAttachment>()
            .HasOne(ma => ma.Message)
            .WithMany(m => m.Attachments)
            .HasForeignKey(ma => ma.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<MessageReadReceipt>()
            .HasOne(mrr => mrr.Message)
            .WithMany(m => m.ReadReceipts)
            .HasForeignKey(mrr => mrr.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<MessageReadReceipt>()
            .HasIndex(mrr => new { mrr.MessageId, mrr.UserId })
            .IsUnique();

    }

}
