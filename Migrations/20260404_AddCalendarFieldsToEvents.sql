-- Migration: 20260404_AddCalendarFieldsToEvents
-- Description: Add Color, AssignedToUserId, Visibility fields to Events table

ALTER TABLE [Events]
    ADD [Color] nvarchar(7) NULL,
        [AssignedToUserId] nvarchar(100) NULL,
        [Visibility] int NOT NULL DEFAULT 0;  -- 0 = Public, 1 = Private

CREATE INDEX [IX_Events_Visibility] ON [Events] ([Visibility]);
CREATE INDEX [IX_Events_AssignedToUserId] ON [Events] ([AssignedToUserId]);
