-- Migration: 20260624_AddAIOrderSettings
-- Adds the AIOrderSettings table for per-tenant AI order assistant configuration.

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AIOrderSettings')
BEGIN
    CREATE TABLE [dbo].[AIOrderSettings] (
        [Id]                          UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]                    UNIQUEIDENTIFIER   NOT NULL,
        [SystemPromptTemplate]        NVARCHAR(4000)     NULL,
        [MaxItemsPerOrder]            INT                NOT NULL DEFAULT 50,
        [RequireConfirmation]         BIT                NOT NULL DEFAULT 1,
        [AutoCreateDocument]          BIT                NOT NULL DEFAULT 1,
        [WelcomeMessage]              NVARCHAR(1000)     NULL,
        [OrderConfirmationTemplate]   NVARCHAR(2000)     NULL,
        [ErrorMessage]                NVARCHAR(500)      NULL,
        [AmbiguousProductMessage]     NVARCHAR(500)      NULL,
        [EnableAI]                    BIT                NOT NULL DEFAULT 0,
        [MaxTokensPerDay]             INT                NOT NULL DEFAULT 0,
        -- Audit fields
        [CreatedAt]                   DATETIME2          NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]                   NVARCHAR(100)      NULL,
        [ModifiedAt]                  DATETIME2          NULL,
        [ModifiedBy]                  NVARCHAR(100)      NULL,
        [IsDeleted]                   BIT                NOT NULL DEFAULT 0,
        [DeletedAt]                   DATETIME2          NULL,
        [DeletedBy]                   NVARCHAR(100)      NULL,
        [IsActive]                    BIT                NOT NULL DEFAULT 1,
        [RowVersion]                  ROWVERSION         NULL,
        CONSTRAINT [PK_AIOrderSettings] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_AIOrderSettings_TenantId]
        ON [dbo].[AIOrderSettings] ([TenantId])
        WHERE [IsDeleted] = 0;

    PRINT 'Created table AIOrderSettings';
END
ELSE
BEGIN
    PRINT 'Table AIOrderSettings already exists — skipped';
END
