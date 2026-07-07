-- ============================================================
-- Migration: 20260707_FixInvalidDocumentTypeRequiredPartyType
-- Fixes DocumentTypes rows whose RequiredPartyType column holds a
-- value outside the entity enum's valid range (Cliente=0,
-- Fornitore=1, ClienteFornitore=2).
--
-- Root cause: before BusinessPartyTypeMapper existed, some code path
-- persisted the DTO enum's raw numeric value (Both=3) directly into
-- this entity-enum column, producing rows with RequiredPartyType=3.
-- That value has no matching entity member, which made
-- BusinessPartyTypeMapper.ToDto throw ArgumentOutOfRangeException and
-- the GET /api/v1/documents/types endpoint return HTTP 500.
--
-- This migration normalizes any out-of-range value to
-- ClienteFornitore (2), the most permissive option and the same
-- semantic meaning ("Both") as the invalid legacy value.
-- ============================================================

BEGIN TRANSACTION;

UPDATE [DocumentTypes]
SET [RequiredPartyType] = 2
WHERE [RequiredPartyType] NOT IN (0, 1, 2);

COMMIT TRANSACTION;
