-- ROLLBACK: 20260624_AddAIOrderSettings
-- Removes the AIOrderSettings table.

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AIOrderSettings')
BEGIN
    DROP TABLE [dbo].[AIOrderSettings];
    PRINT 'Dropped table AIOrderSettings';
END
