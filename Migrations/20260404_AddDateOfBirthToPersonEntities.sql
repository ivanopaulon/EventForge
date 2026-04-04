-- Migration: 20260404_AddDateOfBirthToPersonEntities
-- Description: Add DateOfBirth field to StoreUser, User, and Reference tables

ALTER TABLE [StoreUsers]
    ADD [DateOfBirth] datetime2 NULL;

ALTER TABLE [Users]
    ADD [DateOfBirth] datetime2 NULL;

ALTER TABLE [References]
    ADD [DateOfBirth] datetime2 NULL;

CREATE INDEX [IX_StoreUsers_DateOfBirth] ON [StoreUsers] ([DateOfBirth]) WHERE [DateOfBirth] IS NOT NULL;
CREATE INDEX [IX_Users_DateOfBirth]      ON [Users]      ([DateOfBirth]) WHERE [DateOfBirth] IS NOT NULL;
CREATE INDEX [IX_References_DateOfBirth] ON [References]  ([DateOfBirth]) WHERE [DateOfBirth] IS NOT NULL;
