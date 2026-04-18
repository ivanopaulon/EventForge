-- Migration: Add CashierShifts table for operator shift management
-- Date: 2026-04-16
-- Description: Creates the CashierShifts table to support scheduling and tracking
--              of cash register operator shifts. Includes FK to StoreUsers (operator)
--              and optional FK to StorePoses (register). All AuditableEntity columns included.

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[CashierShifts]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[CashierShifts]
    (
        [Id]          uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]    uniqueidentifier NOT NULL,

        -- Shift-specific columns
        [StoreUserId] uniqueidentifier NOT NULL,
        [PosId]       uniqueidentifier NULL,
        [ShiftStart]  datetime2(7)     NOT NULL,
        [ShiftEnd]    datetime2(7)     NOT NULL,
        [Status]      int              NOT NULL DEFAULT 0,   -- ShiftStatus enum: 0=Scheduled
        [Notes]       nvarchar(500)    NULL,

        -- AuditableEntity columns
        [IsActive]    bit              NOT NULL DEFAULT 1,
        [IsDeleted]   bit              NOT NULL DEFAULT 0,
        [CreatedAt]   datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]   nvarchar(100)    NULL,
        [ModifiedAt]  datetime2(7)     NULL,
        [ModifiedBy]  nvarchar(100)    NULL,
        [DeletedAt]   datetime2(7)     NULL,
        [DeletedBy]   nvarchar(100)    NULL,
        [RowVersion]  rowversion       NOT NULL,

        CONSTRAINT [PK_CashierShifts] PRIMARY KEY ([Id])
    );

    -- Foreign key to StoreUsers (operator)
    ALTER TABLE [dbo].[CashierShifts]
        ADD CONSTRAINT [FK_CashierShifts_StoreUsers_StoreUserId]
        FOREIGN KEY ([StoreUserId])
        REFERENCES [dbo].[StoreUsers] ([Id])
        ON DELETE NO ACTION;

    -- Foreign key to StorePoses (register) — nullable, SET NULL on delete
    ALTER TABLE [dbo].[CashierShifts]
        ADD CONSTRAINT [FK_CashierShifts_StorePoses_PosId]
        FOREIGN KEY ([PosId])
        REFERENCES [dbo].[StorePoses] ([Id])
        ON DELETE SET NULL;

    -- Indexes
    CREATE INDEX [IX_CashierShift_StoreUserId]
        ON [dbo].[CashierShifts] ([StoreUserId])
        WHERE [IsDeleted] = 0;

    CREATE INDEX [IX_CashierShift_PosId]
        ON [dbo].[CashierShifts] ([PosId])
        WHERE [PosId] IS NOT NULL AND [IsDeleted] = 0;

    CREATE INDEX [IX_CashierShift_ShiftStart]
        ON [dbo].[CashierShifts] ([ShiftStart])
        WHERE [IsDeleted] = 0;

    CREATE INDEX [IX_CashierShift_ShiftEnd]
        ON [dbo].[CashierShifts] ([ShiftEnd])
        WHERE [IsDeleted] = 0;

    CREATE INDEX [IX_CashierShift_TenantId_ShiftStart_ShiftEnd]
        ON [dbo].[CashierShifts] ([TenantId], [ShiftStart], [ShiftEnd])
        WHERE [IsDeleted] = 0;

    PRINT 'CashierShifts table created successfully.';
END
ELSE
BEGIN
    PRINT 'CashierShifts table already exists — skipping creation.';
END
