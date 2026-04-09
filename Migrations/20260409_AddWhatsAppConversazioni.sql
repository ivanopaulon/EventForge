-- ============================================================
-- Migration: 20260409_AddWhatsAppConversazioni
-- Description: Add WhatsApp conversation and message tables
-- Date: 2026-04-09
-- ============================================================

-- ConversazioniWhatsApp table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConversazioniWhatsApp')
BEGIN
    CREATE TABLE ConversazioniWhatsApp (
        Id                    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        TenantId              UNIQUEIDENTIFIER NOT NULL,
        NumeroDiTelefono      NVARCHAR(30)     NOT NULL,
        BusinessPartyId       UNIQUEIDENTIFIER NULL,
        Stato                 INT              NOT NULL DEFAULT 0,
        NumeroNonRiconosciuto BIT              NOT NULL DEFAULT 1,
        UltimoMessaggioAt     DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CreatedAt             DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy             NVARCHAR(100)    NULL,
        ModifiedAt            DATETIME2        NULL,
        ModifiedBy            NVARCHAR(100)    NULL,
        IsDeleted             BIT              NOT NULL DEFAULT 0,
        DeletedAt             DATETIME2        NULL,
        DeletedBy             NVARCHAR(100)    NULL,
        IsActive              BIT              NOT NULL DEFAULT 1,
        RowVersion            ROWVERSION       NULL,
        CONSTRAINT PK_ConversazioniWhatsApp PRIMARY KEY (Id),
        CONSTRAINT FK_ConversazioniWhatsApp_BusinessParties
            FOREIGN KEY (BusinessPartyId) REFERENCES BusinessParties(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_ConversazioniWhatsApp_TenantId         ON ConversazioniWhatsApp (TenantId);
    CREATE INDEX IX_ConversazioniWhatsApp_NumeroDiTelefono ON ConversazioniWhatsApp (NumeroDiTelefono);
    CREATE INDEX IX_ConversazioniWhatsApp_Stato            ON ConversazioniWhatsApp (Stato);

    PRINT 'Table ConversazioniWhatsApp created.';
END
ELSE PRINT 'Table ConversazioniWhatsApp already exists, skipped.';

-- MessaggiWhatsApp table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MessaggiWhatsApp')
BEGIN
    CREATE TABLE MessaggiWhatsApp (
        Id                       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        TenantId                 UNIQUEIDENTIFIER NOT NULL,
        ConversazioneWhatsAppId  UNIQUEIDENTIFIER NOT NULL,
        Testo                    NVARCHAR(4000)   NOT NULL DEFAULT '',
        Direzione                INT              NOT NULL DEFAULT 0,
        StatoInvio               INT              NOT NULL DEFAULT 0,
        WhatsAppMessageId        NVARCHAR(200)    NULL,
        MittenteOperatoreId      UNIQUEIDENTIFIER NULL,
        Timestamp                DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        IsLetto                  BIT              NOT NULL DEFAULT 0,
        CreatedAt                DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy                NVARCHAR(100)    NULL,
        ModifiedAt               DATETIME2        NULL,
        ModifiedBy               NVARCHAR(100)    NULL,
        IsDeleted                BIT              NOT NULL DEFAULT 0,
        DeletedAt                DATETIME2        NULL,
        DeletedBy                NVARCHAR(100)    NULL,
        IsActive                 BIT              NOT NULL DEFAULT 1,
        RowVersion               ROWVERSION       NULL,
        CONSTRAINT PK_MessaggiWhatsApp PRIMARY KEY (Id),
        CONSTRAINT FK_MessaggiWhatsApp_Conversazioni
            FOREIGN KEY (ConversazioneWhatsAppId) REFERENCES ConversazioniWhatsApp(Id) ON DELETE CASCADE,
        CONSTRAINT FK_MessaggiWhatsApp_Operatori
            FOREIGN KEY (MittenteOperatoreId) REFERENCES Users(Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_MessaggiWhatsApp_ConversazioneId ON MessaggiWhatsApp (ConversazioneWhatsAppId);
    CREATE INDEX IX_MessaggiWhatsApp_WaMessageId     ON MessaggiWhatsApp (WhatsAppMessageId);
    CREATE INDEX IX_MessaggiWhatsApp_TenantId        ON MessaggiWhatsApp (TenantId);

    PRINT 'Table MessaggiWhatsApp created.';
END
ELSE PRINT 'Table MessaggiWhatsApp already exists, skipped.';

-- NumeriBloccati table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NumeriBloccati')
BEGIN
    CREATE TABLE NumeriBloccati (
        Id               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        TenantId         UNIQUEIDENTIFIER NOT NULL,
        NumeroDiTelefono NVARCHAR(30)     NOT NULL,
        BloccatoAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        Note             NVARCHAR(500)    NULL,
        CreatedAt        DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy        NVARCHAR(100)    NULL,
        ModifiedAt       DATETIME2        NULL,
        ModifiedBy       NVARCHAR(100)    NULL,
        IsDeleted        BIT              NOT NULL DEFAULT 0,
        DeletedAt        DATETIME2        NULL,
        DeletedBy        NVARCHAR(100)    NULL,
        IsActive         BIT              NOT NULL DEFAULT 1,
        RowVersion       ROWVERSION       NULL,
        CONSTRAINT PK_NumeriBloccati PRIMARY KEY (Id),
        CONSTRAINT UX_NumeriBloccati_TenantId_Numero UNIQUE (TenantId, NumeroDiTelefono)
    );

    CREATE INDEX IX_NumeriBloccati_NumeroDiTelefono ON NumeriBloccati (NumeroDiTelefono);

    PRINT 'Table NumeriBloccati created.';
END
ELSE PRINT 'Table NumeriBloccati already exists, skipped.';
GO
