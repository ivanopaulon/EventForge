-- =============================================
-- Migration: Add IsInventoryDocument column to DocumentTypes table
-- Date: 2026-01-28
-- Description: Add IsInventoryDocument boolean column to support flexible inventory document type detection
--              for stock reconciliation calculations, eliminating hardcoded document type codes.
-- =============================================

USE [EventData];
GO

-- Add IsInventoryDocument column if it doesn't exist
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[DocumentTypes]') 
    AND name = 'IsInventoryDocument'
)
BEGIN
    ALTER TABLE [dbo].[DocumentTypes]
    ADD [IsInventoryDocument] bit NOT NULL DEFAULT 0;
    
    PRINT 'Column IsInventoryDocument added to DocumentTypes table.';
END
ELSE
BEGIN
    PRINT 'Column IsInventoryDocument already exists in DocumentTypes table.';
END
GO

-- Update existing inventory document types to set IsInventoryDocument = 1
-- This covers common inventory document type codes
UPDATE [dbo].[DocumentTypes]
SET [IsInventoryDocument] = 1
WHERE [Code] IN ('INVENTORY', 'INV-COUNT', 'STOCK-COUNT', 'INVENT', 'INV', 'INVFIS', 'PHY-INV')
AND [IsInventoryDocument] = 0;

PRINT 'Updated existing inventory document types with IsInventoryDocument = 1.';
GO
