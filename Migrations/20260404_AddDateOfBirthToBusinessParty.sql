-- Migration: 20260404_AddDateOfBirthToBusinessParty
-- Description: Add DateOfBirth field to BusinessParties table (natural-person customers/suppliers)

ALTER TABLE [BusinessParties]
    ADD [DateOfBirth] datetime2 NULL;

CREATE INDEX [IX_BusinessParties_DateOfBirth] ON [BusinessParties] ([DateOfBirth]) WHERE [DateOfBirth] IS NOT NULL;
