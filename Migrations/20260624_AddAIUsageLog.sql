-- Migration: 20260624_AddAIUsageLog
-- Adds the AIUsageLogs table for per-call AI usage and cost tracking.

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AIUsageLogs')
BEGIN
    CREATE TABLE [dbo].[AIUsageLogs] (
        [Id]                  UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]            UNIQUEIDENTIFIER   NOT NULL,
        [ChatThreadId]        UNIQUEIDENTIFIER   NULL,
        [ModelUsed]           NVARCHAR(100)      NULL,
        [TokensUsed]          INT                NOT NULL DEFAULT 0,
        [PromptTokens]        INT                NOT NULL DEFAULT 0,
        [CompletionTokens]    INT                NOT NULL DEFAULT 0,
        [EstimatedCostUsd]    DECIMAL(18,8)      NULL,
        [CallType]            NVARCHAR(100)      NULL,
        [CallAt]              DATETIME2          NOT NULL DEFAULT GETUTCDATE(),
        [Success]             BIT                NOT NULL DEFAULT 1,
        [ErrorMessage]        NVARCHAR(1000)     NULL,
        -- Audit fields
        [CreatedAt]           DATETIME2          NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]           NVARCHAR(100)      NULL,
        [ModifiedAt]          DATETIME2          NULL,
        [ModifiedBy]          NVARCHAR(100)      NULL,
        [IsDeleted]           BIT                NOT NULL DEFAULT 0,
        [DeletedAt]           DATETIME2          NULL,
        [DeletedBy]           NVARCHAR(100)      NULL,
        [IsActive]            BIT                NOT NULL DEFAULT 1,
        [RowVersion]          ROWVERSION         NULL,
        CONSTRAINT [PK_AIUsageLogs] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_AIUsageLogs_TenantId]
        ON [dbo].[AIUsageLogs] ([TenantId]);
    CREATE INDEX [IX_AIUsageLogs_CallAt]
        ON [dbo].[AIUsageLogs] ([CallAt]);
    CREATE INDEX [IX_AIUsageLogs_TenantId_CallAt]
        ON [dbo].[AIUsageLogs] ([TenantId], [CallAt]);

    PRINT 'Created table AIUsageLogs';
END
ELSE
BEGIN
    PRINT 'Table AIUsageLogs already exists — skipped';
END
