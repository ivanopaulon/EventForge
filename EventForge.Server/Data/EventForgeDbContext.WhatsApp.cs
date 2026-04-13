using Prym.DTOs.Chat;
using EventForge.Server.Data.Entities.Chat;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    public DbSet<NumeroBloccato> NumeriBloccati { get; set; }

    private static void ConfigureWhatsAppRelationships(ModelBuilder modelBuilder)
    {
        // NumeroBloccato — blocked WhatsApp numbers registry (independent of ChatThread)
        modelBuilder.Entity<NumeroBloccato>(entity =>
        {
            entity.HasIndex(n => n.NumeroDiTelefono).HasDatabaseName("IX_NumeriBloccati_NumeroDiTelefono");
            entity.HasIndex(n => new { n.TenantId, n.NumeroDiTelefono })
                  .IsUnique()
                  .HasDatabaseName("UX_NumeriBloccati_TenantId_Numero");
        });

        // ChatThread — WhatsApp extensions
        modelBuilder.Entity<ChatThread>(entity =>
        {
            entity.Property(t => t.ExternalPhoneNumber).HasMaxLength(30);
            entity.Property(t => t.WhatsAppLastStatus).HasConversion<int?>();
            entity.HasOne(t => t.BusinessParty)
                  .WithMany()
                  .HasForeignKey(t => t.BusinessPartyId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ChatMessage — WhatsApp extensions
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.Property(m => m.WhatsAppMessageId).HasMaxLength(200);
            entity.Property(m => m.MessageDirection).HasConversion<int?>();
            entity.Property(m => m.WhatsAppDeliveryStatus).HasConversion<int?>();
        });
    }
}
