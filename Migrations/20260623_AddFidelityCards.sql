-- Migration: Add FidelityCards and FidelityPointsTransactions tables
-- Date: 2026-06-23
-- Description: Creates loyalty fidelity cards and points transaction tables.

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityCards]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[FidelityCards]
    (
        [Id]                 uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]           uniqueidentifier NOT NULL,
        [CardNumber]         nvarchar(50)     NOT NULL,
        [Type]               int              NOT NULL DEFAULT 0,
        [Status]             int              NOT NULL DEFAULT 0,
        [ValidFrom]          datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [ValidTo]            datetime2(7)     NOT NULL,
        [CurrentPoints]      int              NOT NULL DEFAULT 0,
        [TotalPointsEarned]  int              NOT NULL DEFAULT 0,
        [TotalPointsRedeemed]int              NOT NULL DEFAULT 0,
        [DiscountPercentage] decimal(5,2)     NOT NULL DEFAULT 0,
        [HasPriorityAccess]  bit              NOT NULL DEFAULT 0,
        [HasBirthdayBonus]   bit              NOT NULL DEFAULT 0,
        [Notes]              nvarchar(500)    NULL,
        [BusinessPartyId]    uniqueidentifier NULL,
        [IsActive]           bit              NOT NULL DEFAULT 1,
        [IsDeleted]          bit              NOT NULL DEFAULT 0,
        [CreatedAt]          datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]          nvarchar(100)    NULL,
        [ModifiedAt]         datetime2(7)     NULL,
        [ModifiedBy]         nvarchar(100)    NULL,
        [DeletedAt]          datetime2(7)     NULL,
        [DeletedBy]          nvarchar(100)    NULL,
        [RowVersion]         rowversion       NOT NULL,
        CONSTRAINT [PK_FidelityCards] PRIMARY KEY ([Id])
    );

    ALTER TABLE [dbo].[FidelityCards]
        ADD CONSTRAINT [FK_FidelityCards_BusinessParties_BusinessPartyId]
        FOREIGN KEY ([BusinessPartyId])
        REFERENCES [dbo].[BusinessParties] ([Id])
        ON DELETE SET NULL;

    PRINT 'FidelityCards table created successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityCards table already exists — skipping creation.';
END

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityPointsTransactions]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[FidelityPointsTransactions]
    (
        [Id]              uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]        uniqueidentifier NOT NULL,
        [FidelityCardId]  uniqueidentifier NOT NULL,
        [TransactionType] int              NOT NULL DEFAULT 0,
        [Points]          int              NOT NULL DEFAULT 0,
        [Description]     nvarchar(200)    NULL,
        [TransactionDate] datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [IsActive]        bit              NOT NULL DEFAULT 1,
        [IsDeleted]       bit              NOT NULL DEFAULT 0,
        [CreatedAt]       datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]       nvarchar(100)    NULL,
        [ModifiedAt]      datetime2(7)     NULL,
        [ModifiedBy]      nvarchar(100)    NULL,
        [DeletedAt]       datetime2(7)     NULL,
        [DeletedBy]       nvarchar(100)    NULL,
        [RowVersion]      rowversion       NOT NULL,
        CONSTRAINT [PK_FidelityPointsTransactions] PRIMARY KEY ([Id])
    );

    ALTER TABLE [dbo].[FidelityPointsTransactions]
        ADD CONSTRAINT [FK_FidelityPointsTransactions_FidelityCards_FidelityCardId]
        FOREIGN KEY ([FidelityCardId])
        REFERENCES [dbo].[FidelityCards] ([Id])
        ON DELETE CASCADE;

    PRINT 'FidelityPointsTransactions table created successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityPointsTransactions table already exists — skipping creation.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityCards_TenantId_CardNumber' AND object_id = OBJECT_ID('dbo.FidelityCards'))
BEGIN
    CREATE UNIQUE INDEX [IX_FidelityCards_TenantId_CardNumber]
        ON [dbo].[FidelityCards] ([TenantId], [CardNumber])
        WHERE [IsDeleted] = 0;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityCards_BusinessPartyId' AND object_id = OBJECT_ID('dbo.FidelityCards'))
BEGIN
    CREATE INDEX [IX_FidelityCards_BusinessPartyId]
        ON [dbo].[FidelityCards] ([BusinessPartyId])
        WHERE [BusinessPartyId] IS NOT NULL AND [IsDeleted] = 0;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityCards_TenantId_Status' AND object_id = OBJECT_ID('dbo.FidelityCards'))
BEGIN
    CREATE INDEX [IX_FidelityCards_TenantId_Status]
        ON [dbo].[FidelityCards] ([TenantId], [Status])
        WHERE [IsDeleted] = 0;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityPointsTransactions_FidelityCardId_TransactionDate' AND object_id = OBJECT_ID('dbo.FidelityPointsTransactions'))
BEGIN
    CREATE INDEX [IX_FidelityPointsTransactions_FidelityCardId_TransactionDate]
        ON [dbo].[FidelityPointsTransactions] ([FidelityCardId], [TransactionDate]);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityPointsTransactions_TenantId' AND object_id = OBJECT_ID('dbo.FidelityPointsTransactions'))
BEGIN
    CREATE INDEX [IX_FidelityPointsTransactions_TenantId]
        ON [dbo].[FidelityPointsTransactions] ([TenantId]);
END

PRINT 'Migration 20260623_AddFidelityCards applied successfully.';
