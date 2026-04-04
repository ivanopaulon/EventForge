-- Migration: 20260404_AddCalendarFieldsToCalendarReminders
-- Description: Add Color, AssignedToUserId, Visibility fields to CalendarReminders table

ALTER TABLE [CalendarReminders]
    ADD [Color] nvarchar(7) NULL,
        [AssignedToUserId] nvarchar(100) NULL,
        [Visibility] int NOT NULL DEFAULT 0;  -- 0 = Public, 1 = Private

CREATE INDEX [IX_CalendarReminders_AssignedToUserId] ON [CalendarReminders] ([AssignedToUserId]);
