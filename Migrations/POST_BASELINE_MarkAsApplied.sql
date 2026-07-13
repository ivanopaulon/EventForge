-- Migration: Mark EF Core InitialBaseline migration as already applied
-- Date: 2026-07-13
-- Description: This script does NOT create or modify any schema object. The schema already
--              exists in full, produced over time by the 126 historical .sql scripts in this
--              folder (see Migrations/README.md). Its only purpose is to seed the
--              [__EFMigrationsHistory] table (the bookkeeping table EF Core Migrations uses to
--              track which migrations have been applied) with a row for the
--              "InitialBaseline" migration, so that EF Core considers it already applied and
--              never attempts to run its Up() (which would otherwise try to CREATE TABLE
--              every table in the schema and fail with "object already exists" errors).
--
-- IMPORTANT:
--   * Do NOT run this automatically / as part of a deployment pipeline.
--   * Run this manually, once, against EACH environment (development, staging, production)
--     that already has the full schema applied via the historical .sql scripts, AFTER you
--     have verified (dotnet ef migrations script) that the InitialBaseline migration
--     generated on that same code revision matches this MigrationId/ProductVersion pair.
--   * Never run this against an environment that does NOT already have the schema (e.g. a
--     brand new/empty database) -- there, use `dotnet ef database update` instead, which will
--     legitimately create the schema from scratch via InitialBaseline.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260713180327_InitialBaseline')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260713180327_InitialBaseline', N'10.0.9');
END;
GO
