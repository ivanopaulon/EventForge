-- Migration: 20260404_AddCalendarReminders
-- Description: Add CalendarReminders table for standalone reminders and tasks

CREATE TABLE [CalendarReminders] (
    [Id] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [TenantId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [DueDate] datetime2 NOT NULL,
    [IsAllDay] bit NOT NULL DEFAULT 0,
    [ItemType] int NOT NULL DEFAULT 0,
    [Priority] int NOT NULL DEFAULT 1,
    [Status] int NOT NULL DEFAULT 0,
    [IsCompleted] bit NOT NULL DEFAULT 0,
    [CompletedAt] datetime2 NULL,
    [CompletedBy] nvarchar(100) NULL,
    [CompletionNotes] nvarchar(500) NULL,
    [EventId] uniqueidentifier NULL,
    [IsRecurring] bit NOT NULL DEFAULT 0,
    [RecurrencePattern] int NULL,
    [RecurrenceInterval] int NULL,
    [RecurrenceEndDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] nvarchar(100) NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] nvarchar(100) NULL,
    [IsDeleted] bit NOT NULL DEFAULT 0,
    [DeletedAt] datetime2 NULL,
    [DeletedBy] nvarchar(100) NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_CalendarReminders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CalendarReminders_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE SET NULL
);

CREATE INDEX [IX_CalendarReminders_TenantId] ON [CalendarReminders] ([TenantId]);
CREATE INDEX [IX_CalendarReminders_DueDate] ON [CalendarReminders] ([DueDate]);
CREATE INDEX [IX_CalendarReminders_TenantId_Status] ON [CalendarReminders] ([TenantId], [Status]);
