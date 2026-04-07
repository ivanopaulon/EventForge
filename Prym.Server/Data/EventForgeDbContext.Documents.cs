using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Data;

public partial class PrymDbContext
{
    private static void ConfigureDocumentRelationships(ModelBuilder modelBuilder)
    {
        // DocumentCounter → DocumentType
        _ = modelBuilder.Entity<DocumentCounter>()
            .HasOne(dc => dc.DocumentType)
            .WithMany()
            .HasForeignKey(dc => dc.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on DocumentType + Series + Year
        _ = modelBuilder.Entity<DocumentCounter>()
            .HasIndex(dc => new { dc.DocumentTypeId, dc.Series, dc.Year })
            .IsUnique()
            .HasDatabaseName("IX_DocumentCounters_DocumentTypeId_Series_Year");

        // DocumentHeader → BusinessParty
        _ = modelBuilder.Entity<DocumentHeader>()
            .HasOne(d => d.BusinessParty)
            .WithMany()
            .HasForeignKey(d => d.BusinessPartyId);

        // DocumentHeader → Address (BusinessPartyAddress)
        _ = modelBuilder.Entity<DocumentHeader>()
            .HasOne(d => d.BusinessPartyAddress)
            .WithMany()
            .HasForeignKey(d => d.BusinessPartyAddressId);

        // DocumentSummaryLink → DocumentHeader (Summary and Detailed)
        _ = modelBuilder.Entity<DocumentSummaryLink>()
            .HasOne(l => l.SummaryDocument)
            .WithMany(h => h.SummaryDocuments)
            .HasForeignKey(l => l.SummaryDocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<DocumentSummaryLink>()
            .HasOne(l => l.DetailedDocument)
            .WithMany(h => h.IncludedInSummaries)
            .HasForeignKey("DetailedDocumentId")
            .OnDelete(DeleteBehavior.Restrict);

        // Document header current workflow execution (optional navigation)
        _ = modelBuilder.Entity<DocumentHeader>()
            .HasOne(dh => dh.CurrentWorkflowExecution)
            .WithMany() // current workflow execution is a single reference, no inverse collection
            .HasForeignKey(dh => dh.CurrentWorkflowExecutionId)
            .OnDelete(DeleteBehavior.SetNull);

        _ = modelBuilder.Entity<DocumentHeader>()
            .HasIndex(dh => dh.CurrentWorkflowExecutionId)
            .HasDatabaseName("IX_DocumentHeaders_CurrentWorkflowExecutionId");

        // DocumentWorkflowExecution → DocumentHeader (history)
        _ = modelBuilder.Entity<DocumentWorkflowExecution>()
            .HasOne(dwe => dwe.DocumentHeader)
            .WithMany(dh => dh.WorkflowExecutions)
            .HasForeignKey(dwe => dwe.DocumentHeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<DocumentWorkflowExecution>()
            .HasIndex(dwe => dwe.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentWorkflowExecutions_DocumentHeaderId");

        // DocumentVersion and reminders/schedules
        _ = modelBuilder.Entity<DocumentVersion>()
            .HasOne(dv => dv.DocumentHeader)
            .WithMany(dh => dh.Versions)
            .HasForeignKey(dv => dv.DocumentHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<DocumentVersion>()
            .HasIndex(dv => dv.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentVersions_DocumentHeaderId");

        _ = modelBuilder.Entity<DocumentReminder>()
            .HasOne(dr => dr.DocumentHeader)
            .WithMany(dh => dh.Reminders)
            .HasForeignKey(dr => dr.DocumentHeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<DocumentReminder>()
            .HasIndex(dr => dr.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentReminders_DocumentHeaderId");

        _ = modelBuilder.Entity<DocumentSchedule>()
            .HasOne(ds => ds.DocumentHeader)
            .WithMany(dh => dh.Schedules)
            .HasForeignKey(ds => ds.DocumentHeaderId)
            .OnDelete(DeleteBehavior.SetNull);

        _ = modelBuilder.Entity<DocumentSchedule>()
            .HasOne(ds => ds.DocumentType)
            .WithMany()
            .HasForeignKey(ds => ds.DocumentTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        _ = modelBuilder.Entity<DocumentSchedule>()
            .HasIndex(ds => ds.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentSchedules_DocumentHeaderId");

        _ = modelBuilder.Entity<DocumentSchedule>()
            .HasIndex(ds => ds.NextExecutionDate)
            .HasDatabaseName("IX_DocumentSchedules_NextExecutionDate");

        // DocumentAccessLog → DocumentHeader: optional to avoid global filter conflicts
        _ = modelBuilder.Entity<DocumentAccessLog>()
            .HasOne(dal => dal.DocumentHeader)
            .WithMany()
            .HasForeignKey(dal => dal.DocumentHeaderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        _ = modelBuilder.Entity<DocumentAccessLog>()
            .HasIndex(dal => dal.DocumentHeaderId)
            .HasDatabaseName("IX_DocumentAccessLogs_DocumentHeaderId");

        // DocumentReference: ignore polymorphic navigation to keep model simple
        _ = modelBuilder.Entity<DocumentReference>()
            .Ignore(d => d.Team)
            .Ignore(d => d.TeamMember);

        // MembershipCard → DocumentReference
        _ = modelBuilder.Entity<MembershipCard>()
            .HasOne(mc => mc.DocumentReference)
            .WithMany()
            .HasForeignKey(mc => mc.DocumentReferenceId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigurePriceApplicationMode(ModelBuilder modelBuilder)
    {
        // BusinessParty price application mode configuration
        _ = modelBuilder.Entity<BusinessParty>()
            .Property(bp => bp.DefaultPriceApplicationMode)
            .HasConversion<int>()
            .IsRequired();

        _ = modelBuilder.Entity<BusinessParty>()
            .HasOne(bp => bp.ForcedPriceList)
            .WithMany()
            .HasForeignKey(bp => bp.ForcedPriceListId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<BusinessParty>()
            .HasIndex(bp => bp.ForcedPriceListId)
            .HasDatabaseName("IX_BusinessParties_ForcedPriceListId");

        // DocumentHeader price application mode override configuration
        _ = modelBuilder.Entity<DocumentHeader>()
            .Property(dh => dh.PriceApplicationModeOverride)
            .HasConversion<int?>();

        // DocumentRow price tracking configuration
        _ = modelBuilder.Entity<DocumentRow>()
            .Property(dr => dr.IsPriceManual)
            .IsRequired();

        _ = modelBuilder.Entity<DocumentRow>()
            .Property(dr => dr.OriginalPriceFromPriceList)
            .HasPrecision(18, 4);

        _ = modelBuilder.Entity<DocumentRow>()
            .Property(dr => dr.PriceNotes)
            .HasMaxLength(500);

        _ = modelBuilder.Entity<DocumentRow>()
            .HasOne(dr => dr.AppliedPriceList)
            .WithMany()
            .HasForeignKey(dr => dr.AppliedPriceListId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = modelBuilder.Entity<DocumentRow>()
            .HasIndex(dr => dr.AppliedPriceListId)
            .HasDatabaseName("IX_DocumentRows_AppliedPriceListId");

        _ = modelBuilder.Entity<DocumentRow>()
            .Property(dr => dr.AppliedPromotionsJSON)
            .HasMaxLength(4000)
            .IsRequired(false);
    }

}
