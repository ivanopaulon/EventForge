-- Rollback: Remove InsurancePolicies table
-- Date: 2026-07-11

IF EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[InsurancePolicies]') AND type IN (N'U')
)
BEGIN
    DROP TABLE [dbo].[InsurancePolicies];
    PRINT 'InsurancePolicies table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'InsurancePolicies table does not exist — nothing to roll back.';
END
