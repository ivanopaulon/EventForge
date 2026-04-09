-- ============================================================
-- Migration: 20260409_UnifyWhatsAppIntoChatThread
-- Description: Extend ChatThreads and ChatMessages with nullable
--              WhatsApp fields, replacing the separate
--              ConversazioniWhatsApp / MessaggiWhatsApp tables.
-- Date: 2026-04-09
-- ============================================================

-- ── 1. Extend ChatThreads ──────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatThreads') AND name = 'ExternalPhoneNumber')
BEGIN
    ALTER TABLE ChatThreads ADD ExternalPhoneNumber NVARCHAR(30) NULL;
    PRINT 'ChatThreads.ExternalPhoneNumber added.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatThreads') AND name = 'BusinessPartyId')
BEGIN
    ALTER TABLE ChatThreads ADD BusinessPartyId UNIQUEIDENTIFIER NULL;
    ALTER TABLE ChatThreads ADD CONSTRAINT FK_ChatThreads_BusinessParties
        FOREIGN KEY (BusinessPartyId) REFERENCES BusinessParties(Id) ON DELETE SET NULL;
    PRINT 'ChatThreads.BusinessPartyId added.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatThreads') AND name = 'IsUnrecognizedNumber')
BEGIN
    ALTER TABLE ChatThreads ADD IsUnrecognizedNumber BIT NOT NULL DEFAULT 0;
    PRINT 'ChatThreads.IsUnrecognizedNumber added.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatThreads') AND name = 'WhatsAppLastStatus')
BEGIN
    ALTER TABLE ChatThreads ADD WhatsAppLastStatus INT NULL;
    PRINT 'ChatThreads.WhatsAppLastStatus added.';
END;

-- Index for quick lookup by phone number
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ChatThreads') AND name = 'IX_ChatThreads_ExternalPhoneNumber')
BEGIN
    CREATE INDEX IX_ChatThreads_ExternalPhoneNumber ON ChatThreads (ExternalPhoneNumber);
    PRINT 'Index IX_ChatThreads_ExternalPhoneNumber created.';
END;

-- ── 2. Extend ChatMessages ─────────────────────────────────────────────────

-- SenderId is now nullable (incoming WhatsApp messages have no internal sender)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatMessages') AND name = 'SenderId'
           AND is_nullable = 0)
BEGIN
    ALTER TABLE ChatMessages ALTER COLUMN SenderId UNIQUEIDENTIFIER NULL;
    PRINT 'ChatMessages.SenderId made nullable.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatMessages') AND name = 'WhatsAppMessageId')
BEGIN
    ALTER TABLE ChatMessages ADD WhatsAppMessageId NVARCHAR(200) NULL;
    PRINT 'ChatMessages.WhatsAppMessageId added.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatMessages') AND name = 'MessageDirection')
BEGIN
    ALTER TABLE ChatMessages ADD MessageDirection INT NULL;  -- 0=Entrante, 1=Uscente
    PRINT 'ChatMessages.MessageDirection added.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ChatMessages') AND name = 'WhatsAppDeliveryStatus')
BEGIN
    ALTER TABLE ChatMessages ADD WhatsAppDeliveryStatus INT NULL;  -- 0=Inviato,1=Consegnato,2=Letto,3=Errore
    PRINT 'ChatMessages.WhatsAppDeliveryStatus added.';
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('ChatMessages') AND name = 'IX_ChatMessages_WhatsAppMessageId')
BEGIN
    CREATE INDEX IX_ChatMessages_WhatsAppMessageId ON ChatMessages (WhatsAppMessageId);
    PRINT 'Index IX_ChatMessages_WhatsAppMessageId created.';
END;

-- ── 3. Data migration (optional): copy existing ConversazioniWhatsApp rows ──
-- Uncomment this block if there is existing data to migrate before dropping tables.
/*
INSERT INTO ChatThreads
    (Id, TenantId, [Type], [Name], ExternalPhoneNumber, BusinessPartyId,
     IsUnrecognizedNumber, WhatsAppLastStatus, IsPrivate, UpdatedAt,
     CreatedAt, CreatedBy, IsDeleted, IsActive)
SELECT
    Id, TenantId, 3 AS [Type],
    ISNULL(NumeroDiTelefono, CAST(Id AS NVARCHAR(40))) AS [Name],
    NumeroDiTelefono, BusinessPartyId, NumeroNonRiconosciuto,
    Stato, 1 AS IsPrivate, UltimoMessaggioAt,
    CreatedAt, CreatedBy, IsDeleted, IsActive
FROM ConversazioniWhatsApp
WHERE Id NOT IN (SELECT Id FROM ChatThreads);

INSERT INTO ChatMessages
    (Id, TenantId, ChatThreadId, Content, SenderId, MessageDirection,
     WhatsAppDeliveryStatus, WhatsAppMessageId, [Status], SentAt,
     CreatedAt, CreatedBy, IsDeleted, IsActive)
SELECT
    m.Id, m.TenantId, m.ConversazioneWhatsAppId AS ChatThreadId,
    m.Testo AS Content, m.MittenteOperatoreId AS SenderId,
    m.Direzione AS MessageDirection, m.StatoInvio AS WhatsAppDeliveryStatus,
    m.WhatsAppMessageId, 1 AS [Status], m.Timestamp AS SentAt,
    m.CreatedAt, m.CreatedBy, m.IsDeleted, m.IsActive
FROM MessaggiWhatsApp m
WHERE m.Id NOT IN (SELECT Id FROM ChatMessages);
*/

-- ── 4. Drop obsolete tables (run AFTER data migration above) ──────────────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MessaggiWhatsApp')
BEGIN
    DROP TABLE MessaggiWhatsApp;
    PRINT 'Table MessaggiWhatsApp dropped.';
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConversazioniWhatsApp')
BEGIN
    DROP TABLE ConversazioniWhatsApp;
    PRINT 'Table ConversazioniWhatsApp dropped.';
END;

GO
