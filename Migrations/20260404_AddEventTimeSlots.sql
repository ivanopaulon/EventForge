-- Migration: 20260404_AddEventTimeSlots
-- Description: Create the EventTimeSlots table with daily time-slot support per Event.
--              Each row defines a daily start/end time (e.g. 08:00–12:00) that applies
--              to every day spanned by the parent event's date range.
--              Events may have zero or more slots; zero slots means the event is treated
--              as all-day (or the times are encoded directly in Event.StartDate/EndDate).

CREATE TABLE [EventTimeSlots] (
    [Id]        uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [EventId]   uniqueidentifier NOT NULL,
    [StartTime] time             NOT NULL,
    [EndTime]   time             NOT NULL,
    [Label]     nvarchar(100)    NULL,
    [SortOrder] int              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_EventTimeSlots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventTimeSlots_Events_EventId]
        FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
);

-- Index for fast lookup by event
CREATE INDEX [IX_EventTimeSlots_EventId]
    ON [EventTimeSlots] ([EventId]);

-- Index for ordered display per event
CREATE INDEX [IX_EventTimeSlots_EventId_SortOrder]
    ON [EventTimeSlots] ([EventId], [SortOrder]);

-- ROLLBACK --
-- DROP TABLE IF EXISTS [EventTimeSlots];
