-- Migration: Add StationOrderQueueItems table for station monitor FIFO queues
-- Date: 2026-06-23
-- Description: Creates the StationOrderQueueItems table including all auditable columns,
--              queue metadata, and supporting foreign keys / indexes.

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[StationOrderQueueItems]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[StationOrderQueueItems]
    (
        [Id]               uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]         uniqueidentifier NOT NULL,
        [StationId]        uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NOT NULL,
        [DocumentRowId]    uniqueidentifier NULL,
        [TeamMemberId]     uniqueidentifier NULL,
        [ProductId]        uniqueidentifier NOT NULL,
        [Quantity]         int              NOT NULL DEFAULT 1,
        [Status]           int              NOT NULL DEFAULT 0, -- Waiting
        [SortOrder]        int              NOT NULL DEFAULT 0,
        [AssignedAt]       datetime2(7)     NULL,
        [StartedAt]        datetime2(7)     NULL,
        [CompletedAt]      datetime2(7)     NULL,
        [Notes]            nvarchar(200)    NULL,

        -- AuditableEntity columns
        [IsActive]         bit              NOT NULL DEFAULT 1,
        [IsDeleted]        bit              NOT NULL DEFAULT 0,
        [CreatedAt]        datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]        nvarchar(100)    NULL,
        [ModifiedAt]       datetime2(7)     NULL,
        [ModifiedBy]       nvarchar(100)    NULL,
        [DeletedAt]        datetime2(7)     NULL,
        [DeletedBy]        nvarchar(100)    NULL,
        [RowVersion]       rowversion       NOT NULL,

        CONSTRAINT [PK_StationOrderQueueItems] PRIMARY KEY ([Id])
    );

    ALTER TABLE [dbo].[StationOrderQueueItems]
        ADD CONSTRAINT [FK_StationOrderQueueItems_Stations_StationId]
        FOREIGN KEY ([StationId]) REFERENCES [dbo].[Stations] ([Id]) ON DELETE NO ACTION;

    ALTER TABLE [dbo].[StationOrderQueueItems]
        ADD CONSTRAINT [FK_StationOrderQueueItems_DocumentHeaders_DocumentHeaderId]
        FOREIGN KEY ([DocumentHeaderId]) REFERENCES [dbo].[DocumentHeaders] ([Id]) ON DELETE NO ACTION;

    ALTER TABLE [dbo].[StationOrderQueueItems]
        ADD CONSTRAINT [FK_StationOrderQueueItems_DocumentRows_DocumentRowId]
        FOREIGN KEY ([DocumentRowId]) REFERENCES [dbo].[DocumentRows] ([Id]) ON DELETE NO ACTION;

    ALTER TABLE [dbo].[StationOrderQueueItems]
        ADD CONSTRAINT [FK_StationOrderQueueItems_TeamMembers_TeamMemberId]
        FOREIGN KEY ([TeamMemberId]) REFERENCES [dbo].[TeamMembers] ([Id]) ON DELETE NO ACTION;

    ALTER TABLE [dbo].[StationOrderQueueItems]
        ADD CONSTRAINT [FK_StationOrderQueueItems_Products_ProductId]
        FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION;

    CREATE INDEX [IX_StationOrderQueueItems_StationId_Status_SortOrder]
        ON [dbo].[StationOrderQueueItems] ([StationId], [Status], [SortOrder])
        WHERE [IsDeleted] = 0;

    CREATE INDEX [IX_StationOrderQueueItems_TenantId_StationId]
        ON [dbo].[StationOrderQueueItems] ([TenantId], [StationId])
        WHERE [IsDeleted] = 0;

    CREATE INDEX [IX_StationOrderQueueItems_DocumentHeaderId]
        ON [dbo].[StationOrderQueueItems] ([DocumentHeaderId])
        WHERE [IsDeleted] = 0;

    CREATE INDEX [IX_StationOrderQueueItems_ProductId]
        ON [dbo].[StationOrderQueueItems] ([ProductId])
        WHERE [IsDeleted] = 0;

    PRINT 'StationOrderQueueItems table created successfully.';
END
ELSE
BEGIN
    PRINT 'StationOrderQueueItems table already exists — skipping creation.';
END
