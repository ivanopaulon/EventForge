-- ============================================================
-- Migration: 20260407_AddFiscalDrawer
-- Adds FiscalDrawers, FiscalDrawerSessions,
-- FiscalDrawerTransactions, and CashDenominations tables.
-- ============================================================

-- 1. FiscalDrawers
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FiscalDrawers')
BEGIN
    CREATE TABLE [FiscalDrawers] (
        [Id]             uniqueidentifier NOT NULL DEFAULT NEWID(),
        [TenantId]       uniqueidentifier NOT NULL,
        [Name]           nvarchar(100)    NOT NULL,
        [Code]           nvarchar(20)     NULL,
        [Description]    nvarchar(200)    NULL,
        [AssignmentType] int              NOT NULL DEFAULT 0,
        [CurrencyCode]   nvarchar(3)      NOT NULL DEFAULT N'EUR',
        [Status]         int              NOT NULL DEFAULT 0,
        [OpeningBalance] decimal(18,2)    NOT NULL DEFAULT 0,
        [CurrentBalance] decimal(18,2)    NOT NULL DEFAULT 0,
        [PosId]          uniqueidentifier NULL,
        [OperatorId]     uniqueidentifier NULL,
        [Notes]          nvarchar(200)    NULL,
        [IsDeleted]      bit              NOT NULL DEFAULT 0,
        [IsActive]       bit              NOT NULL DEFAULT 1,
        [DeletedAt]      datetime2        NULL,
        [DeletedBy]      nvarchar(100)    NULL,
        [CreatedAt]      datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]      nvarchar(100)    NULL,
        [ModifiedAt]     datetime2        NULL,
        [ModifiedBy]     nvarchar(100)    NULL,
        [RowVersion]     rowversion       NULL,
        CONSTRAINT [PK_FiscalDrawers] PRIMARY KEY ([Id])
    );
END
GO

-- FiscalDrawers indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawer_TenantId' AND object_id = OBJECT_ID('FiscalDrawers'))
    CREATE INDEX [IX_FiscalDrawer_TenantId] ON [FiscalDrawers] ([TenantId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawer_PosId' AND object_id = OBJECT_ID('FiscalDrawers'))
    CREATE INDEX [IX_FiscalDrawer_PosId] ON [FiscalDrawers] ([PosId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawer_OperatorId' AND object_id = OBJECT_ID('FiscalDrawers'))
    CREATE INDEX [IX_FiscalDrawer_OperatorId] ON [FiscalDrawers] ([OperatorId]);
GO

-- FiscalDrawers FKs
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FiscalDrawers_StorePoses_PosId')
    ALTER TABLE [FiscalDrawers] ADD CONSTRAINT [FK_FiscalDrawers_StorePoses_PosId]
        FOREIGN KEY ([PosId]) REFERENCES [StorePoses] ([Id]) ON DELETE SET NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_FiscalDrawers_StoreUsers_OperatorId')
    ALTER TABLE [FiscalDrawers] ADD CONSTRAINT [FK_FiscalDrawers_StoreUsers_OperatorId]
        FOREIGN KEY ([OperatorId]) REFERENCES [StoreUsers] ([Id]) ON DELETE SET NULL;
GO

-- 2. FiscalDrawerSessions
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FiscalDrawerSessions')
BEGIN
    CREATE TABLE [FiscalDrawerSessions] (
        [Id]                    uniqueidentifier NOT NULL DEFAULT NEWID(),
        [TenantId]              uniqueidentifier NOT NULL,
        [FiscalDrawerId]        uniqueidentifier NOT NULL,
        [SessionDate]           date             NOT NULL,
        [OpenedAt]              datetime2        NOT NULL,
        [ClosedAt]              datetime2        NULL,
        [OpeningBalance]        decimal(18,2)    NOT NULL DEFAULT 0,
        [ClosingBalance]        decimal(18,2)    NOT NULL DEFAULT 0,
        [TotalCashIn]           decimal(18,2)    NOT NULL DEFAULT 0,
        [TotalCashOut]          decimal(18,2)    NOT NULL DEFAULT 0,
        [TotalSales]            decimal(18,2)    NOT NULL DEFAULT 0,
        [TotalDeposits]         decimal(18,2)    NOT NULL DEFAULT 0,
        [TotalWithdrawals]      decimal(18,2)    NOT NULL DEFAULT 0,
        [TransactionCount]      int              NOT NULL DEFAULT 0,
        [OpenedByOperatorId]    uniqueidentifier NULL,
        [ClosedByOperatorId]    uniqueidentifier NULL,
        [Status]                int              NOT NULL DEFAULT 0,
        [Notes]                 nvarchar(500)    NULL,
        [IsDeleted]             bit              NOT NULL DEFAULT 0,
        [IsActive]              bit              NOT NULL DEFAULT 1,
        [DeletedAt]             datetime2        NULL,
        [DeletedBy]             nvarchar(100)    NULL,
        [CreatedAt]             datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]             nvarchar(100)    NULL,
        [ModifiedAt]            datetime2        NULL,
        [ModifiedBy]            nvarchar(100)    NULL,
        [RowVersion]            rowversion       NULL,
        CONSTRAINT [PK_FiscalDrawerSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FiscalDrawerSessions_FiscalDrawers_FiscalDrawerId]
            FOREIGN KEY ([FiscalDrawerId]) REFERENCES [FiscalDrawers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FiscalDrawerSessions_StoreUsers_OpenedBy]
            FOREIGN KEY ([OpenedByOperatorId]) REFERENCES [StoreUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_FiscalDrawerSessions_StoreUsers_ClosedBy]
            FOREIGN KEY ([ClosedByOperatorId]) REFERENCES [StoreUsers] ([Id]) ON DELETE SET NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawerSession_FiscalDrawerId' AND object_id = OBJECT_ID('FiscalDrawerSessions'))
    CREATE INDEX [IX_FiscalDrawerSession_FiscalDrawerId] ON [FiscalDrawerSessions] ([FiscalDrawerId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawerSession_SessionDate' AND object_id = OBJECT_ID('FiscalDrawerSessions'))
    CREATE INDEX [IX_FiscalDrawerSession_SessionDate] ON [FiscalDrawerSessions] ([SessionDate]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawerSession_TenantId' AND object_id = OBJECT_ID('FiscalDrawerSessions'))
    CREATE INDEX [IX_FiscalDrawerSession_TenantId] ON [FiscalDrawerSessions] ([TenantId]);
GO

-- 3. FiscalDrawerTransactions
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FiscalDrawerTransactions')
BEGIN
    CREATE TABLE [FiscalDrawerTransactions] (
        [Id]                      uniqueidentifier NOT NULL DEFAULT NEWID(),
        [TenantId]                uniqueidentifier NOT NULL,
        [FiscalDrawerId]          uniqueidentifier NOT NULL,
        [FiscalDrawerSessionId]   uniqueidentifier NULL,
        [TransactionType]         int              NOT NULL,
        [PaymentType]             int              NOT NULL DEFAULT 0,
        [Amount]                  decimal(18,2)    NOT NULL,
        [Description]             nvarchar(200)    NULL,
        [SaleSessionId]           uniqueidentifier NULL,
        [TransactionAt]           datetime2        NOT NULL,
        [OperatorName]            nvarchar(100)    NULL,
        [IsDeleted]               bit              NOT NULL DEFAULT 0,
        [IsActive]                bit              NOT NULL DEFAULT 1,
        [DeletedAt]               datetime2        NULL,
        [DeletedBy]               nvarchar(100)    NULL,
        [CreatedAt]               datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]               nvarchar(100)    NULL,
        [ModifiedAt]              datetime2        NULL,
        [ModifiedBy]              nvarchar(100)    NULL,
        [RowVersion]              rowversion       NULL,
        CONSTRAINT [PK_FiscalDrawerTransactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FiscalDrawerTransactions_FiscalDrawers_FiscalDrawerId]
            FOREIGN KEY ([FiscalDrawerId]) REFERENCES [FiscalDrawers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_FiscalDrawerTransactions_FiscalDrawerSessions_SessionId]
            FOREIGN KEY ([FiscalDrawerSessionId]) REFERENCES [FiscalDrawerSessions] ([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawerTransaction_FiscalDrawerId' AND object_id = OBJECT_ID('FiscalDrawerTransactions'))
    CREATE INDEX [IX_FiscalDrawerTransaction_FiscalDrawerId] ON [FiscalDrawerTransactions] ([FiscalDrawerId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawerTransaction_TransactionAt' AND object_id = OBJECT_ID('FiscalDrawerTransactions'))
    CREATE INDEX [IX_FiscalDrawerTransaction_TransactionAt] ON [FiscalDrawerTransactions] ([TransactionAt]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FiscalDrawerTransaction_TenantId' AND object_id = OBJECT_ID('FiscalDrawerTransactions'))
    CREATE INDEX [IX_FiscalDrawerTransaction_TenantId] ON [FiscalDrawerTransactions] ([TenantId]);
GO

-- 4. CashDenominations
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CashDenominations')
BEGIN
    CREATE TABLE [CashDenominations] (
        [Id]               uniqueidentifier NOT NULL DEFAULT NEWID(),
        [TenantId]         uniqueidentifier NOT NULL,
        [FiscalDrawerId]   uniqueidentifier NOT NULL,
        [CurrencyCode]     nvarchar(3)      NOT NULL DEFAULT N'EUR',
        [Value]            decimal(18,4)    NOT NULL,
        [DenominationType] int              NOT NULL DEFAULT 0,
        [Quantity]         int              NOT NULL DEFAULT 0,
        [SortOrder]        int              NOT NULL DEFAULT 0,
        [IsDeleted]        bit              NOT NULL DEFAULT 0,
        [IsActive]         bit              NOT NULL DEFAULT 1,
        [DeletedAt]        datetime2        NULL,
        [DeletedBy]        nvarchar(100)    NULL,
        [CreatedAt]        datetime2        NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]        nvarchar(100)    NULL,
        [ModifiedAt]       datetime2        NULL,
        [ModifiedBy]       nvarchar(100)    NULL,
        [RowVersion]       rowversion       NULL,
        CONSTRAINT [PK_CashDenominations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CashDenominations_FiscalDrawers_FiscalDrawerId]
            FOREIGN KEY ([FiscalDrawerId]) REFERENCES [FiscalDrawers] ([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CashDenomination_FiscalDrawerId' AND object_id = OBJECT_ID('CashDenominations'))
    CREATE INDEX [IX_CashDenomination_FiscalDrawerId] ON [CashDenominations] ([FiscalDrawerId]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CashDenomination_TenantId' AND object_id = OBJECT_ID('CashDenominations'))
    CREATE INDEX [IX_CashDenomination_TenantId] ON [CashDenominations] ([TenantId]);
GO

-- Unique index: one Code per tenant (allows multiple NULLs since NULLs are distinct in SQL Server)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UIX_FiscalDrawer_TenantId_Code' AND object_id = OBJECT_ID('FiscalDrawers'))
    CREATE UNIQUE INDEX [UIX_FiscalDrawer_TenantId_Code]
        ON [FiscalDrawers] ([TenantId], [Code])
        WHERE [Code] IS NOT NULL AND [IsDeleted] = 0;
GO
