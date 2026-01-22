-- Migration: Add Price Application Modes
-- Date: 2026-01-22
-- Description: Add price application mode support to BusinessParty, DocumentHeader, and DocumentRow

-- 1. Add columns to BusinessParties table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BusinessParties]') AND name = 'DefaultPriceApplicationMode')
BEGIN
    ALTER TABLE [dbo].[BusinessParties]
    ADD [DefaultPriceApplicationMode] INT NOT NULL DEFAULT 0; -- 0 = Automatic

    PRINT 'Added DefaultPriceApplicationMode to BusinessParties';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[BusinessParties]') AND name = 'ForcedPriceListId')
BEGIN
    ALTER TABLE [dbo].[BusinessParties]
    ADD [ForcedPriceListId] UNIQUEIDENTIFIER NULL;

    -- Add foreign key constraint
    ALTER TABLE [dbo].[BusinessParties]
    ADD CONSTRAINT [FK_BusinessParties_PriceLists_ForcedPriceListId]
        FOREIGN KEY ([ForcedPriceListId])
        REFERENCES [dbo].[PriceLists] ([Id])
        ON DELETE NO ACTION;

    -- Add index
    CREATE NONCLUSTERED INDEX [IX_BusinessParties_ForcedPriceListId]
        ON [dbo].[BusinessParties]([ForcedPriceListId] ASC);

    PRINT 'Added ForcedPriceListId to BusinessParties';
END

-- 2. Add columns to DocumentHeaders table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DocumentHeaders]') AND name = 'PriceApplicationModeOverride')
BEGIN
    ALTER TABLE [dbo].[DocumentHeaders]
    ADD [PriceApplicationModeOverride] INT NULL;

    PRINT 'Added PriceApplicationModeOverride to DocumentHeaders';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DocumentHeaders]') AND name = 'ForcedPriceListIdOverride')
BEGIN
    ALTER TABLE [dbo].[DocumentHeaders]
    ADD [ForcedPriceListIdOverride] UNIQUEIDENTIFIER NULL;

    -- Add foreign key constraint
    ALTER TABLE [dbo].[DocumentHeaders]
    ADD CONSTRAINT [FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride]
        FOREIGN KEY ([ForcedPriceListIdOverride])
        REFERENCES [dbo].[PriceLists] ([Id])
        ON DELETE NO ACTION;

    -- Add index
    CREATE NONCLUSTERED INDEX [IX_DocumentHeaders_ForcedPriceListIdOverride]
        ON [dbo].[DocumentHeaders]([ForcedPriceListIdOverride] ASC);

    PRINT 'Added ForcedPriceListIdOverride to DocumentHeaders';
END

-- 3. Add columns to DocumentRows table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DocumentRows]') AND name = 'IsPriceManual')
BEGIN
    ALTER TABLE [dbo].[DocumentRows]
    ADD [IsPriceManual] BIT NOT NULL DEFAULT 0;

    PRINT 'Added IsPriceManual to DocumentRows';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DocumentRows]') AND name = 'AppliedPriceListId')
BEGIN
    ALTER TABLE [dbo].[DocumentRows]
    ADD [AppliedPriceListId] UNIQUEIDENTIFIER NULL;

    -- Add foreign key constraint
    ALTER TABLE [dbo].[DocumentRows]
    ADD CONSTRAINT [FK_DocumentRows_PriceLists_AppliedPriceListId]
        FOREIGN KEY ([AppliedPriceListId])
        REFERENCES [dbo].[PriceLists] ([Id])
        ON DELETE NO ACTION;

    -- Add index
    CREATE NONCLUSTERED INDEX [IX_DocumentRows_AppliedPriceListId]
        ON [dbo].[DocumentRows]([AppliedPriceListId] ASC);

    PRINT 'Added AppliedPriceListId to DocumentRows';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DocumentRows]') AND name = 'OriginalPriceFromPriceList')
BEGIN
    ALTER TABLE [dbo].[DocumentRows]
    ADD [OriginalPriceFromPriceList] DECIMAL(18, 4) NULL;

    PRINT 'Added OriginalPriceFromPriceList to DocumentRows';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DocumentRows]') AND name = 'PriceNotes')
BEGIN
    ALTER TABLE [dbo].[DocumentRows]
    ADD [PriceNotes] NVARCHAR(500) NULL;

    PRINT 'Added PriceNotes to DocumentRows';
END

PRINT 'Migration AddPriceApplicationModes completed successfully';
GO
