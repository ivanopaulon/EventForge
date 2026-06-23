-- ============================================================
-- Rollback: 20260623_AddImageDocumentToStoreEntities
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_StoreUserPrivileges_DocumentReferences_ImageDocumentId'
)
BEGIN
    ALTER TABLE [StoreUserPrivileges] DROP CONSTRAINT [FK_StoreUserPrivileges_DocumentReferences_ImageDocumentId];
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StoreUserPrivilege_ImageDocumentId' AND object_id = OBJECT_ID('StoreUserPrivileges')
)
BEGIN
    DROP INDEX [IX_StoreUserPrivilege_ImageDocumentId] ON [StoreUserPrivileges];
END
GO

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'StoreUserPrivileges' AND COLUMN_NAME = 'ImageDocumentId'
)
BEGIN
    ALTER TABLE [StoreUserPrivileges] DROP COLUMN [ImageDocumentId];
END
GO

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_StoreUsers_DocumentReferences_PhotoDocumentId'
)
BEGIN
    ALTER TABLE [StoreUsers] DROP CONSTRAINT [FK_StoreUsers_DocumentReferences_PhotoDocumentId];
END
GO

ALTER TABLE [StoreUsers]
    ADD CONSTRAINT [FK_StoreUsers_DocumentReferences_PhotoDocumentId]
    FOREIGN KEY ([PhotoDocumentId]) REFERENCES [DocumentReferences] ([Id])
    ON DELETE NO ACTION;
GO

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_StoreUserGroups_DocumentReferences_LogoDocumentId'
)
BEGIN
    ALTER TABLE [StoreUserGroups] DROP CONSTRAINT [FK_StoreUserGroups_DocumentReferences_LogoDocumentId];
END
GO

ALTER TABLE [StoreUserGroups]
    ADD CONSTRAINT [FK_StoreUserGroups_DocumentReferences_LogoDocumentId]
    FOREIGN KEY ([LogoDocumentId]) REFERENCES [DocumentReferences] ([Id])
    ON DELETE NO ACTION;
GO

IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_StorePoses_DocumentReferences_ImageDocumentId'
)
BEGIN
    ALTER TABLE [StorePoses] DROP CONSTRAINT [FK_StorePoses_DocumentReferences_ImageDocumentId];
END
GO

ALTER TABLE [StorePoses]
    ADD CONSTRAINT [FK_StorePoses_DocumentReferences_ImageDocumentId]
    FOREIGN KEY ([ImageDocumentId]) REFERENCES [DocumentReferences] ([Id])
    ON DELETE NO ACTION;
GO
