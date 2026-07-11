-- Migration: Add InsurancePolicies table
-- Date: 2026-07-11
-- Description: Creates the InsurancePolicies table backing InsurancePolicy entity
-- (insurance coverage history for TeamMembers), idempotent pattern.

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[InsurancePolicies]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[InsurancePolicies]
    (
        [Id]                   uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TeamMemberId]         uniqueidentifier NOT NULL,
        [Provider]             nvarchar(200)    NOT NULL,
        [PolicyNumber]         nvarchar(100)    NOT NULL,
        [ValidFrom]            datetime2(7)     NOT NULL,
        [ValidTo]              datetime2(7)     NOT NULL,
        [DocumentReferenceId]  uniqueidentifier NULL,
        [CoverageType]         nvarchar(100)    NULL,
        [CoverageAmount]       decimal(18,2)    NULL,
        [Currency]             nvarchar(3)      NULL DEFAULT 'EUR',
        [Notes]                nvarchar(1000)   NULL,
        [TenantId]             uniqueidentifier NOT NULL,
        [CreatedAt]            datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]            nvarchar(100)    NULL,
        [ModifiedAt]           datetime2(7)     NULL,
        [ModifiedBy]           nvarchar(100)    NULL,
        [IsDeleted]            bit              NOT NULL DEFAULT 0,
        [DeletedAt]            datetime2(7)     NULL,
        [DeletedBy]            nvarchar(100)    NULL,
        [IsActive]             bit              NOT NULL DEFAULT 1,
        [RowVersion]           rowversion       NOT NULL,
        CONSTRAINT [PK_InsurancePolicies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InsurancePolicies_TeamMembers_TeamMemberId] FOREIGN KEY ([TeamMemberId]) REFERENCES [dbo].[TeamMembers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_InsurancePolicies_DocumentReferences_DocumentReferenceId] FOREIGN KEY ([DocumentReferenceId]) REFERENCES [dbo].[DocumentReferences] ([Id]) ON DELETE SET NULL
    );

    PRINT 'InsurancePolicies table created successfully.';
END
ELSE
BEGIN
    PRINT 'InsurancePolicies table already exists — skipping creation.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InsurancePolicies_TeamMemberId' AND object_id = OBJECT_ID('dbo.InsurancePolicies'))
BEGIN
    CREATE INDEX [IX_InsurancePolicies_TeamMemberId]
        ON [dbo].[InsurancePolicies] ([TeamMemberId])
        WHERE [IsDeleted] = 0;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InsurancePolicies_DocumentReferenceId' AND object_id = OBJECT_ID('dbo.InsurancePolicies'))
BEGIN
    CREATE INDEX [IX_InsurancePolicies_DocumentReferenceId]
        ON [dbo].[InsurancePolicies] ([DocumentReferenceId])
        WHERE [DocumentReferenceId] IS NOT NULL;
END

PRINT 'Migration 20260711_AddInsurancePolicies applied successfully.';
