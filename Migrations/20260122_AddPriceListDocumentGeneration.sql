-- Add columns to PriceLists table for document-based generation
-- Migration for FASE 2C - PR #4: Price list generation from purchase documents

-- Add IsGeneratedFromDocuments flag
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PriceLists') AND name = 'IsGeneratedFromDocuments')
BEGIN
    ALTER TABLE PriceLists 
    ADD IsGeneratedFromDocuments BIT NOT NULL DEFAULT 0;
END
GO

-- Add GenerationMetadata JSON field
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PriceLists') AND name = 'GenerationMetadata')
BEGIN
    ALTER TABLE PriceLists 
    ADD GenerationMetadata NVARCHAR(MAX) NULL;
END
GO

-- Add LastSyncedAt timestamp
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PriceLists') AND name = 'LastSyncedAt')
BEGIN
    ALTER TABLE PriceLists 
    ADD LastSyncedAt DATETIME2 NULL;
END
GO

-- Add LastSyncedBy user tracking
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PriceLists') AND name = 'LastSyncedBy')
BEGIN
    ALTER TABLE PriceLists 
    ADD LastSyncedBy NVARCHAR(256) NULL;
END
GO

-- Add index for query performance on generated price lists
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('PriceLists') AND name = 'IX_PriceLists_IsGeneratedFromDocuments')
BEGIN
    CREATE INDEX IX_PriceLists_IsGeneratedFromDocuments 
    ON PriceLists(TenantId, IsGeneratedFromDocuments, LastSyncedAt)
    WHERE IsGeneratedFromDocuments = 1;
END
GO
