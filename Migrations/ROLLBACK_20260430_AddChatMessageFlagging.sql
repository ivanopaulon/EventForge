-- ROLLBACK: Remove message flagging columns from ChatMessages
-- Date: 2026-04-30
-- Description: Reverses 20260430_AddChatMessageFlagging.sql

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = N'IX_ChatMessages_IsFlagged_TenantId'
)
BEGIN
    DROP INDEX [IX_ChatMessages_IsFlagged_TenantId] ON [dbo].[ChatMessages];
    PRINT 'Dropped index IX_ChatMessages_IsFlagged_TenantId.';
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = N'IsFlagged'
)
BEGIN
    ALTER TABLE [dbo].[ChatMessages]
        DROP COLUMN [IsFlagged], [FlaggedAt], [FlaggedBy], [FlagReason];

    PRINT 'Removed IsFlagged, FlaggedAt, FlaggedBy, FlagReason from ChatMessages.';
END
GO
