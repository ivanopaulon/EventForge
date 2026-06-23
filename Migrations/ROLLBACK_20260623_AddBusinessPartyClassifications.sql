IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BusinessPartyClassifications')
BEGIN
    DROP TABLE [BusinessPartyClassifications];
END
GO
