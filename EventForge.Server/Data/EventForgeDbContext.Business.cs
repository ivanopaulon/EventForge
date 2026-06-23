using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    private static void ConfigureBusinessRelationships(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<FidelityCard>()
            .Property(card => card.DiscountPercentage)
            .HasPrecision(5, 2);

        _ = modelBuilder.Entity<FidelityCard>()
            .HasOne(card => card.BusinessParty)
            .WithMany()
            .HasForeignKey(card => card.BusinessPartyId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        _ = modelBuilder.Entity<FidelityCard>()
            .HasIndex(card => new { card.TenantId, card.CardNumber })
            .HasDatabaseName("IX_FidelityCards_TenantId_CardNumber")
            .HasFilter("[IsDeleted] = 0")
            .IsUnique();

        _ = modelBuilder.Entity<FidelityCard>()
            .HasIndex(card => card.BusinessPartyId)
            .HasDatabaseName("IX_FidelityCards_BusinessPartyId");

        _ = modelBuilder.Entity<FidelityCard>()
            .HasIndex(card => new { card.TenantId, card.Status })
            .HasDatabaseName("IX_FidelityCards_TenantId_Status");

        _ = modelBuilder.Entity<FidelityPointsTransaction>()
            .HasOne(transaction => transaction.FidelityCard)
            .WithMany(card => card.Transactions)
            .HasForeignKey(transaction => transaction.FidelityCardId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<FidelityPointsTransaction>()
            .HasIndex(transaction => new { transaction.FidelityCardId, transaction.TransactionDate })
            .HasDatabaseName("IX_FidelityPointsTransactions_FidelityCardId_TransactionDate");

        _ = modelBuilder.Entity<FidelityPointsTransaction>()
            .HasIndex(transaction => transaction.TenantId)
            .HasDatabaseName("IX_FidelityPointsTransactions_TenantId");
    }
}
