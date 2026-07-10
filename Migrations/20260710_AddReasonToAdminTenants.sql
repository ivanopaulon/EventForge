-- Migration: AdminTenant hardening — solo aggiunta motivazione, ExpiresAt resta invariato (nullable)
-- Date: 2026-07-10
-- Description: Adds optional Reason column to AdminTenants. No changes to ExpiresAt: it stays
--              nullable at the schema level; existing rows are not touched. Mandatory Reason/ExpiresAt
--              is enforced only at the application level for grants created from now on
--              (see TenantsController.AddTenantAdmin / TenantService.AddTenantAdminAsync).

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[AdminTenants]') AND name = 'Reason'
)
BEGIN
    ALTER TABLE [dbo].[AdminTenants] ADD [Reason] nvarchar(500) NULL;
    PRINT 'Reason column added to AdminTenants.';
END
ELSE
BEGIN
    PRINT 'Reason column already exists on AdminTenants — skipping.';
END
-- Nessun ALTER su ExpiresAt: resta nullable, le righe esistenti non vengono toccate.

PRINT 'Migration 20260710_AddReasonToAdminTenants applied successfully.';
