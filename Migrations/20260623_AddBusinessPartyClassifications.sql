-- ============================================================
-- Migration: 20260623_AddBusinessPartyClassifications
-- Creates the BusinessPartyClassifications join table.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'BusinessPartyClassifications'
)
BEGIN
    CREATE TABLE [BusinessPartyClassifications] (
        [Id]                   uniqueidentifier   NOT NULL DEFAULT NEWSEQUENTIALID(),
        [BusinessPartyId]      uniqueidentifier   NOT NULL,
        [ClassificationNodeId] uniqueidentifier   NOT NULL,
        [TenantId]             uniqueidentifier   NOT NULL,
        [CreatedAt]            datetime2(7)       NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]            nvarchar(100)      NULL,
        [ModifiedAt]           datetime2(7)       NULL,
        [ModifiedBy]           nvarchar(100)      NULL,
        [IsDeleted]            bit                NOT NULL DEFAULT 0,
        [DeletedAt]            datetime2(7)       NULL,
        [DeletedBy]            nvarchar(100)      NULL,
        [IsActive]             bit                NOT NULL DEFAULT 1,
        [RowVersion]           rowversion         NOT NULL,
        CONSTRAINT [PK_BusinessPartyClassifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BPC_BusinessParties] FOREIGN KEY ([BusinessPartyId])
            REFERENCES [BusinessParties] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BPC_ClassificationNodes] FOREIGN KEY ([ClassificationNodeId])
            REFERENCES [ClassificationNodes] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_BPC_BusinessPartyId]
        ON [BusinessPartyClassifications] ([BusinessPartyId])
        WHERE [IsDeleted] = 0;

    CREATE INDEX [IX_BPC_ClassificationNodeId]
        ON [BusinessPartyClassifications] ([ClassificationNodeId])
        WHERE [IsDeleted] = 0;

    CREATE UNIQUE INDEX [UX_BPC_BusinessParty_Node_Tenant]
        ON [BusinessPartyClassifications] ([BusinessPartyId], [ClassificationNodeId], [TenantId])
        WHERE [IsDeleted] = 0;
END
GO
