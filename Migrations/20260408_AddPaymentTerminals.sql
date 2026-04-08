-- Migration: Add PaymentTerminals table and DefaultPaymentTerminalId to StorePos
-- Date: 2026-04-08

CREATE TABLE PaymentTerminals (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(200) NULL,
    IsEnabled BIT NOT NULL DEFAULT 1,
    ConnectionType NVARCHAR(20) NOT NULL DEFAULT 'Tcp',
    IpAddress NVARCHAR(45) NULL,
    Port INT NOT NULL DEFAULT 60000,
    AgentId UNIQUEIDENTIFIER NULL,
    TimeoutMs INT NOT NULL DEFAULT 30000,
    AmountConfirmationRequired BIT NOT NULL DEFAULT 0,
    TerminalId NVARCHAR(8) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(256) NULL,
    ModifiedAt DATETIME2 NULL,
    ModifiedBy NVARCHAR(256) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL,
    DeletedBy NVARCHAR(256) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_PaymentTerminals PRIMARY KEY (Id)
);

CREATE INDEX IX_PaymentTerminals_TenantId ON PaymentTerminals (TenantId);

ALTER TABLE StorePos
    ADD DefaultPaymentTerminalId UNIQUEIDENTIFIER NULL;

ALTER TABLE StorePos
    ADD CONSTRAINT FK_StorePos_PaymentTerminals_DefaultPaymentTerminalId
    FOREIGN KEY (DefaultPaymentTerminalId) REFERENCES PaymentTerminals(Id) ON DELETE SET NULL;

CREATE INDEX IX_StorePos_DefaultPaymentTerminalId ON StorePos (DefaultPaymentTerminalId);
