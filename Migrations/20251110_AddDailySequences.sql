-- Migration: Add DailySequences table and Products.Code unique constraint
-- Date: 2025-11-10
-- Description: Adds infrastructure for automatic server-side Product.Code generation
--              with format YYYYMMDDNNNNNN (UTC date + 6-digit daily counter)

-- Create DailySequences table for tracking daily code generation counters
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DailySequences]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DailySequences]
    (
        [Date] date NOT NULL PRIMARY KEY,
        [LastNumber] bigint NOT NULL DEFAULT 0
    );
    
    PRINT 'Created table DailySequences';
END
GO

-- Add unique constraint on Products.Code
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Products_Code' AND object_id = OBJECT_ID('dbo.Products'))
BEGIN
    CREATE UNIQUE INDEX [UQ_Products_Code] ON [dbo].[Products]([Code])
    WHERE [Code] IS NOT NULL AND [Code] <> '';
    
    PRINT 'Created unique index UQ_Products_Code on Products.Code';
END
GO

-- Verify migration
SELECT 
    'DailySequences' AS TableName,
    CASE WHEN EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DailySequences]') AND type in (N'U'))
        THEN 'Created' 
        ELSE 'Not Found' 
    END AS Status
UNION ALL
SELECT 
    'UQ_Products_Code Index' AS TableName,
    CASE WHEN EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Products_Code' AND object_id = OBJECT_ID('dbo.Products'))
        THEN 'Created'
        ELSE 'Not Found'
    END AS Status;
GO

/*
NOTES:
------
1. The DailySequences table uses 'date' column type for efficient date-based lookups
2. The LastNumber starts at 0 and is incremented atomically using SQL Server row locks
3. The unique constraint on Products.Code prevents duplicate codes but allows NULL/empty values
4. Code generation is handled by the DailySequentialCodeGenerator service using UPDLOCK, ROWLOCK
5. If a product is created with a Code, no automatic generation occurs
6. Once created, the Product.Code field is immutable (UpdateProductDto doesn't include Code)

ROLLBACK (if needed):
----------------------
-- DROP INDEX [UQ_Products_Code] ON [dbo].[Products];
-- DROP TABLE [dbo].[DailySequences];
*/
