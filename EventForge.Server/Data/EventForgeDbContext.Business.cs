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

        _ = modelBuilder.Entity<FidelityPointsBaseRate>()
            .Property(rate => rate.Rate)
            .HasPrecision(18, 6);

        _ = modelBuilder.Entity<FidelityPointsBaseRate>()
            .HasIndex(rate => new { rate.TenantId, rate.EffectiveFrom })
            .HasDatabaseName("IX_FidelityPointsBaseRates_TenantId_EffectiveFrom")
            .HasFilter("[IsDeleted] = 0");

        _ = modelBuilder.Entity<FidelityTierMultiplier>()
            .Property(multiplier => multiplier.Multiplier)
            .HasPrecision(18, 6);

        _ = modelBuilder.Entity<FidelityTierMultiplier>()
            .HasIndex(multiplier => new { multiplier.TenantId, multiplier.CardType })
            .HasDatabaseName("IX_FidelityTierMultipliers_TenantId_CardType")
            .HasFilter("[IsDeleted] = 0")
            .IsUnique();

        _ = modelBuilder.Entity<FidelityPointsCampaign>()
            .Property(campaign => campaign.Multiplier)
            .HasPrecision(18, 6);

        _ = modelBuilder.Entity<FidelityPointsCampaign>()
            .HasIndex(campaign => new { campaign.TenantId, campaign.StartDate, campaign.EndDate })
            .HasDatabaseName("IX_FidelityPointsCampaigns_TenantId_StartDate_EndDate")
            .HasFilter("[IsDeleted] = 0");
    }
}
