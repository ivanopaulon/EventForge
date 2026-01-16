-- Migration: Create DocumentStatusHistories table
-- Date: 2026-01-16
-- Description: Add DocumentStatusHistory table for audit trail of document status changes

-- Create DocumentStatusHistories table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentStatusHistories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DocumentStatusHistories] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [DocumentHeaderId] UNIQUEIDENTIFIER NOT NULL,
        [FromStatus] INT NOT NULL,
        [ToStatus] INT NOT NULL,
        [Reason] NVARCHAR(500) NULL,
        [ChangedBy] NVARCHAR(256) NOT NULL,
        [ChangedAt] DATETIME2 NOT NULL,
        [IpAddress] NVARCHAR(45) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        [MetadataJson] NVARCHAR(MAX) NULL,
        [TenantId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [ModifiedAt] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(100) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        [DeletedAt] DATETIME2 NULL,
        [DeletedBy] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [RowVersion] ROWVERSION NOT NULL,
        
        CONSTRAINT [PK_DocumentStatusHistories] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_DocumentStatusHistories_DocumentHeaders_DocumentHeaderId] 
            FOREIGN KEY ([DocumentHeaderId]) 
            REFERENCES [dbo].[DocumentHeaders] ([Id]) 
            ON DELETE CASCADE
    );

    -- Create indexes
    CREATE NONCLUSTERED INDEX [IX_DocumentStatusHistories_DocumentHeaderId] 
        ON [dbo].[DocumentStatusHistories]([DocumentHeaderId] ASC);
    
    CREATE NONCLUSTERED INDEX [IX_DocumentStatusHistories_TenantId] 
        ON [dbo].[DocumentStatusHistories]([TenantId] ASC);
    
    CREATE NONCLUSTERED INDEX [IX_DocumentStatusHistories_ChangedAt] 
        ON [dbo].[DocumentStatusHistories]([ChangedAt] DESC);

    PRINT 'DocumentStatusHistories table created successfully';
END
ELSE
BEGIN
    PRINT 'DocumentStatusHistories table already exists';
END
GO
