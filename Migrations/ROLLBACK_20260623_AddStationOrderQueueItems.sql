-- Rollback: Remove StationOrderQueueItems table
-- Date: 2026-06-23
-- Rolls back migration 20260623_AddStationOrderQueueItems.sql

IF EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[StationOrderQueueItems]') AND type IN (N'U')
)
BEGIN
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StationOrderQueueItems_Stations_StationId')
        ALTER TABLE [dbo].[StationOrderQueueItems] DROP CONSTRAINT [FK_StationOrderQueueItems_Stations_StationId];

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StationOrderQueueItems_DocumentHeaders_DocumentHeaderId')
        ALTER TABLE [dbo].[StationOrderQueueItems] DROP CONSTRAINT [FK_StationOrderQueueItems_DocumentHeaders_DocumentHeaderId];

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StationOrderQueueItems_DocumentRows_DocumentRowId')
        ALTER TABLE [dbo].[StationOrderQueueItems] DROP CONSTRAINT [FK_StationOrderQueueItems_DocumentRows_DocumentRowId];

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StationOrderQueueItems_TeamMembers_TeamMemberId')
        ALTER TABLE [dbo].[StationOrderQueueItems] DROP CONSTRAINT [FK_StationOrderQueueItems_TeamMembers_TeamMemberId];

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StationOrderQueueItems_Products_ProductId')
        ALTER TABLE [dbo].[StationOrderQueueItems] DROP CONSTRAINT [FK_StationOrderQueueItems_Products_ProductId];

    DROP TABLE [dbo].[StationOrderQueueItems];

    PRINT 'StationOrderQueueItems table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'StationOrderQueueItems table does not exist — nothing to roll back.';
END
