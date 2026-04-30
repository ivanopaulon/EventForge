-- Migration: Add message flagging / moderation columns to ChatMessages
-- Date: 2026-04-30
-- Description: Adds IsFlagged, FlaggedAt, FlaggedBy and FlagReason to [dbo].[ChatMessages]
--              to support the "Report message" feature in the chat UI.
--              These columns allow users to flag inappropriate messages for moderator review.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = N'IsFlagged'
)
BEGIN
    ALTER TABLE [dbo].[ChatMessages]
        ADD [IsFlagged]  bit           NOT NULL DEFAULT 0,
            [FlaggedAt]  datetime2(7)  NULL,
            [FlaggedBy]  nvarchar(100) NULL,
            [FlagReason] nvarchar(500) NULL;

    PRINT 'Added IsFlagged, FlaggedAt, FlaggedBy, FlagReason to ChatMessages.';
END
ELSE
BEGIN
    PRINT 'Columns already exist on ChatMessages — skipping.';
END
GO

-- Index to allow moderators to quickly list all flagged messages per tenant
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = N'IX_ChatMessages_IsFlagged_TenantId'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ChatMessages_IsFlagged_TenantId]
        ON [dbo].[ChatMessages] ([IsFlagged], [TenantId])
        WHERE [IsFlagged] = 1;

    PRINT 'Created index IX_ChatMessages_IsFlagged_TenantId.';
END
GO
