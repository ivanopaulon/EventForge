using EventForge.Server.Data.Entities.Chat;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    public DbSet<ConversazioneWhatsApp> ConversazioniWhatsApp { get; set; }
    public DbSet<MessaggioWhatsApp> MessaggiWhatsApp { get; set; }
    public DbSet<NumeroBloccato> NumeriBloccati { get; set; }

    private static void ConfigureWhatsAppRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversazioneWhatsApp>(entity =>
        {
            entity.HasIndex(c => c.NumeroDiTelefono).HasDatabaseName("IX_ConversazioniWhatsApp_NumeroDiTelefono");
            entity.HasIndex(c => c.TenantId).HasDatabaseName("IX_ConversazioniWhatsApp_TenantId");
            entity.HasIndex(c => c.Stato).HasDatabaseName("IX_ConversazioniWhatsApp_Stato");
            entity.HasOne(c => c.BusinessParty)
                  .WithMany()
                  .HasForeignKey(c => c.BusinessPartyId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MessaggioWhatsApp>(entity =>
        {
            entity.HasIndex(m => m.ConversazioneWhatsAppId).HasDatabaseName("IX_MessaggiWhatsApp_ConversazioneId");
            entity.HasIndex(m => m.WhatsAppMessageId).HasDatabaseName("IX_MessaggiWhatsApp_WaMessageId");
            entity.HasIndex(m => m.TenantId).HasDatabaseName("IX_MessaggiWhatsApp_TenantId");
            entity.HasOne(m => m.ConversazioneWhatsApp)
                  .WithMany(c => c.Messaggi)
                  .HasForeignKey(m => m.ConversazioneWhatsAppId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(m => m.MittenteOperatore)
                  .WithMany()
                  .HasForeignKey(m => m.MittenteOperatoreId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NumeroBloccato>(entity =>
        {
            entity.HasIndex(n => n.NumeroDiTelefono).HasDatabaseName("IX_NumeriBloccati_NumeroDiTelefono");
            entity.HasIndex(n => new { n.TenantId, n.NumeroDiTelefono })
                  .IsUnique()
                  .HasDatabaseName("UX_NumeriBloccati_TenantId_Numero");
        });
    }
}
