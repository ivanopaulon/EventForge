using EventForge.Server.Data.Entities.Calendar;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Data;

public partial class EventForgeDbContext
{
    private static void ConfigureEventTimeSlotRelationships(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<EventTimeSlot>()
            .HasQueryFilter(s => !s.Event.IsDeleted);

        _ = modelBuilder.Entity<EventTimeSlot>()
            .HasOne(s => s.Event)
            .WithMany(e => e.TimeSlots)
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = modelBuilder.Entity<EventTimeSlot>()
            .HasIndex(s => s.EventId)
            .HasDatabaseName("IX_EventTimeSlots_EventId");

        _ = modelBuilder.Entity<EventTimeSlot>()
            .HasIndex(s => new { s.EventId, s.SortOrder })
            .HasDatabaseName("IX_EventTimeSlots_EventId_SortOrder");
    }

    private static void ConfigureCalendarReminderRelationships(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<CalendarReminder>()
            .HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_CalendarReminders_TenantId");

        _ = modelBuilder.Entity<CalendarReminder>()
            .HasIndex(r => r.DueDate)
            .HasDatabaseName("IX_CalendarReminders_DueDate");

        _ = modelBuilder.Entity<CalendarReminder>()
            .HasIndex(r => new { r.TenantId, r.Status })
            .HasDatabaseName("IX_CalendarReminders_TenantId_Status");

        _ = modelBuilder.Entity<CalendarReminder>()
            .HasOne(r => r.Event)
            .WithMany()
            .HasForeignKey(r => r.EventId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }

}
