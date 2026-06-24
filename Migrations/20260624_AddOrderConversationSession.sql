-- Migration: 20260624_AddOrderConversationSession
-- Adds the OrderConversationSessions table for AI-driven WhatsApp order conversations.

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OrderConversationSessions')
BEGIN
    CREATE TABLE [dbo].[OrderConversationSessions] (
        [Id]                       UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]                 UNIQUEIDENTIFIER   NOT NULL,
        [ChatThreadId]             UNIQUEIDENTIFIER   NOT NULL,
        [BusinessPartyId]          UNIQUEIDENTIFIER   NULL,
        [State]                    INT                NOT NULL DEFAULT 0,   -- 0=Idle, 1=CollectingItems, 2=ConfirmingOrder, 3=Completed, 4=Cancelled
        [DraftJson]                NVARCHAR(16000)    NULL,
        [LastAiPromptAt]           DATETIME2          NULL,
        [CreatedDocumentHeaderId]  UNIQUEIDENTIFIER   NULL,
        [AiRoundCount]             INT                NOT NULL DEFAULT 0,
        -- Audit fields (AuditableEntity)
        [CreatedAt]                DATETIME2          NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]                NVARCHAR(100)      NULL,
        [ModifiedAt]               DATETIME2          NULL,
        [ModifiedBy]               NVARCHAR(100)      NULL,
        [IsDeleted]                BIT                NOT NULL DEFAULT 0,
        [DeletedAt]                DATETIME2          NULL,
        [DeletedBy]                NVARCHAR(100)      NULL,
        [IsActive]                 BIT                NOT NULL DEFAULT 1,
        [RowVersion]               ROWVERSION         NULL,
        CONSTRAINT [PK_OrderConversationSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderConversationSessions_ChatThreads] FOREIGN KEY ([ChatThreadId])
            REFERENCES [dbo].[ChatThreads] ([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_OrderConversationSessions_TenantId]
        ON [dbo].[OrderConversationSessions] ([TenantId]);
    -- Composite index covers single-column ChatThreadId lookups (no separate single-column index needed)
    CREATE INDEX [IX_OrderConversationSessions_ChatThreadId_TenantId]
        ON [dbo].[OrderConversationSessions] ([ChatThreadId], [TenantId]);

    PRINT 'Created table OrderConversationSessions';
END
ELSE
BEGIN
    PRINT 'Table OrderConversationSessions already exists — skipped';
END
