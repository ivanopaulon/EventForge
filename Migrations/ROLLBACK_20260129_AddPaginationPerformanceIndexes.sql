-- =============================================
-- Rollback Migration: Add Pagination Performance Indexes
-- Date: 2026-01-29
-- Description: Removes all indexes created by 20260129_AddPaginationPerformanceIndexes.sql
-- =============================================

USE [EventData];
GO

PRINT 'Rolling back Pagination Performance Indexes...';
GO

-- =============================================
-- CATEGORY 5: Covering Indexes (2)
-- =============================================

PRINT 'Removing Category 5: Covering Indexes...';
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentHeaders_Covering' AND object_id = OBJECT_ID('DocumentHeaders'))
BEGIN
    DROP INDEX IX_DocumentHeaders_Covering ON DocumentHeaders;
    PRINT '  ✓ Dropped IX_DocumentHeaders_Covering';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Covering' AND object_id = OBJECT_ID('Products'))
BEGIN
    DROP INDEX IX_Products_Covering ON Products;
    PRINT '  ✓ Dropped IX_Products_Covering';
END
GO

-- =============================================
-- CATEGORY 4: Composite Filter Indexes (8)
-- =============================================

PRINT 'Removing Category 4: Composite Filter Indexes...';
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserRoles_RoleName_UserId' AND object_id = OBJECT_ID('UserRoles'))
BEGIN
    DROP INDEX IX_UserRoles_RoleName_UserId ON UserRoles;
    PRINT '  ✓ Dropped IX_UserRoles_RoleName_UserId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_TenantId_Unread' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    DROP INDEX IX_Notifications_TenantId_Unread ON Notifications;
    PRINT '  ✓ Dropped IX_Notifications_TenantId_Unread';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LogEntries_TenantId_Level_Timestamp' AND object_id = OBJECT_ID('LogEntries'))
BEGIN
    DROP INDEX IX_LogEntries_TenantId_Level_Timestamp ON LogEntries;
    PRINT '  ✓ Dropped IX_LogEntries_TenantId_Level_Timestamp';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditTrails_TenantId_UserId_Timestamp' AND object_id = OBJECT_ID('AuditTrails'))
BEGIN
    DROP INDEX IX_AuditTrails_TenantId_UserId_Timestamp ON AuditTrails;
    PRINT '  ✓ Dropped IX_AuditTrails_TenantId_UserId_Timestamp';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditTrails_TenantId_EntityType_Timestamp' AND object_id = OBJECT_ID('AuditTrails'))
BEGIN
    DROP INDEX IX_AuditTrails_TenantId_EntityType_Timestamp ON AuditTrails;
    PRINT '  ✓ Dropped IX_AuditTrails_TenantId_EntityType_Timestamp';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_TenantId_Active' AND object_id = OBJECT_ID('Products'))
BEGIN
    DROP INDEX IX_Products_TenantId_Active ON Products;
    PRINT '  ✓ Dropped IX_Products_TenantId_Active';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TableSessions_TenantId_Available' AND object_id = OBJECT_ID('TableSessions'))
BEGIN
    DROP INDEX IX_TableSessions_TenantId_Available ON TableSessions;
    PRINT '  ✓ Dropped IX_TableSessions_TenantId_Available';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleSessions_TenantId_OpenSessions' AND object_id = OBJECT_ID('SaleSessions'))
BEGIN
    DROP INDEX IX_SaleSessions_TenantId_OpenSessions ON SaleSessions;
    PRINT '  ✓ Dropped IX_SaleSessions_TenantId_OpenSessions';
END
GO

-- =============================================
-- CATEGORY 3: Foreign Key Indexes (15)
-- =============================================

PRINT 'Removing Category 3: Foreign Key Indexes...';
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_SenderId' AND object_id = OBJECT_ID('ChatMessages'))
BEGIN
    DROP INDEX IX_ChatMessages_SenderId ON ChatMessages;
    PRINT '  ✓ Dropped IX_ChatMessages_SenderId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_ThreadId' AND object_id = OBJECT_ID('ChatMessages'))
BEGIN
    DROP INDEX IX_ChatMessages_ThreadId ON ChatMessages;
    PRINT '  ✓ Dropped IX_ChatMessages_ThreadId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClassificationNodes_ParentId' AND object_id = OBJECT_ID('ClassificationNodes'))
BEGIN
    DROP INDEX IX_ClassificationNodes_ParentId ON ClassificationNodes;
    PRINT '  ✓ Dropped IX_ClassificationNodes_ParentId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageLocations_StorageFacilityId' AND object_id = OBJECT_ID('StorageLocations'))
BEGIN
    DROP INDEX IX_StorageLocations_StorageFacilityId ON StorageLocations;
    PRINT '  ✓ Dropped IX_StorageLocations_StorageFacilityId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Lots_StorageLocationId' AND object_id = OBJECT_ID('Lots'))
BEGIN
    DROP INDEX IX_Lots_StorageLocationId ON Lots;
    PRINT '  ✓ Dropped IX_Lots_StorageLocationId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Lots_ProductId' AND object_id = OBJECT_ID('Lots'))
BEGIN
    DROP INDEX IX_Lots_ProductId ON Lots;
    PRINT '  ✓ Dropped IX_Lots_ProductId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_ProductId' AND object_id = OBJECT_ID('SaleItems'))
BEGIN
    DROP INDEX IX_SaleItems_ProductId ON SaleItems;
    PRINT '  ✓ Dropped IX_SaleItems_ProductId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_SaleSessionId' AND object_id = OBJECT_ID('SaleItems'))
BEGIN
    DROP INDEX IX_SaleItems_SaleSessionId ON SaleItems;
    PRINT '  ✓ Dropped IX_SaleItems_SaleSessionId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentRows_ProductId' AND object_id = OBJECT_ID('DocumentRows'))
BEGIN
    DROP INDEX IX_DocumentRows_ProductId ON DocumentRows;
    PRINT '  ✓ Dropped IX_DocumentRows_ProductId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentRows_DocumentHeaderId' AND object_id = OBJECT_ID('DocumentRows'))
BEGIN
    DROP INDEX IX_DocumentRows_DocumentHeaderId ON DocumentRows;
    PRINT '  ✓ Dropped IX_DocumentRows_DocumentHeaderId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Models_BrandId' AND object_id = OBJECT_ID('Models'))
BEGIN
    DROP INDEX IX_Models_BrandId ON Models;
    PRINT '  ✓ Dropped IX_Models_BrandId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_BrandId' AND object_id = OBJECT_ID('Products'))
BEGIN
    DROP INDEX IX_Products_BrandId ON Products;
    PRINT '  ✓ Dropped IX_Products_BrandId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_BaseUnitOfMeasureId' AND object_id = OBJECT_ID('Products'))
BEGIN
    DROP INDEX IX_Products_BaseUnitOfMeasureId ON Products;
    PRINT '  ✓ Dropped IX_Products_BaseUnitOfMeasureId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_SupplierId' AND object_id = OBJECT_ID('Products'))
BEGIN
    DROP INDEX IX_Products_SupplierId ON Products;
    PRINT '  ✓ Dropped IX_Products_SupplierId';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CategoryId' AND object_id = OBJECT_ID('Products'))
BEGIN
    DROP INDEX IX_Products_CategoryId ON Products;
    PRINT '  ✓ Dropped IX_Products_CategoryId';
END
GO

-- =============================================
-- CATEGORY 2: DateTime Sorting Indexes (8)
-- =============================================

PRINT 'Removing Category 2: DateTime Sorting Indexes...';
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EntityChangeLogs_TenantId_Timestamp' AND object_id = OBJECT_ID('EntityChangeLogs'))
BEGIN
    DROP INDEX IX_EntityChangeLogs_TenantId_Timestamp ON EntityChangeLogs;
    PRINT '  ✓ Dropped IX_EntityChangeLogs_TenantId_Timestamp';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_TenantId_CreatedAt' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    DROP INDEX IX_Notifications_TenantId_CreatedAt ON Notifications;
    PRINT '  ✓ Dropped IX_Notifications_TenantId_CreatedAt';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_TenantId_SentAt' AND object_id = OBJECT_ID('ChatMessages'))
BEGIN
    DROP INDEX IX_ChatMessages_TenantId_SentAt ON ChatMessages;
    PRINT '  ✓ Dropped IX_ChatMessages_TenantId_SentAt';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LogEntries_TenantId_Timestamp' AND object_id = OBJECT_ID('LogEntries'))
BEGIN
    DROP INDEX IX_LogEntries_TenantId_Timestamp ON LogEntries;
    PRINT '  ✓ Dropped IX_LogEntries_TenantId_Timestamp';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditTrails_TenantId_Timestamp' AND object_id = OBJECT_ID('AuditTrails'))
BEGIN
    DROP INDEX IX_AuditTrails_TenantId_Timestamp ON AuditTrails;
    PRINT '  ✓ Dropped IX_AuditTrails_TenantId_Timestamp';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleSessions_TenantId_CreatedAt' AND object_id = OBJECT_ID('SaleSessions'))
BEGIN
    DROP INDEX IX_SaleSessions_TenantId_CreatedAt ON SaleSessions;
    PRINT '  ✓ Dropped IX_SaleSessions_TenantId_CreatedAt';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_TenantId_EventDate' AND object_id = OBJECT_ID('Events'))
BEGIN
    DROP INDEX IX_Events_TenantId_EventDate ON Events;
    PRINT '  ✓ Dropped IX_Events_TenantId_EventDate';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentHeaders_TenantId_DocumentDate' AND object_id = OBJECT_ID('DocumentHeaders'))
BEGIN
    DROP INDEX IX_DocumentHeaders_TenantId_DocumentDate ON DocumentHeaders;
    PRINT '  ✓ Dropped IX_DocumentHeaders_TenantId_DocumentDate';
END
GO

-- =============================================
-- CATEGORY 1: Core Pagination Indexes (26)
-- =============================================

PRINT 'Removing Category 1: Core Pagination Indexes...';
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BusinessParties_TenantId_IsDeleted' AND object_id = OBJECT_ID('BusinessParties'))
BEGIN
    DROP INDEX IX_BusinessParties_TenantId_IsDeleted ON BusinessParties;
    PRINT '  ✓ Dropped IX_BusinessParties_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentHeaders_TenantId_IsDeleted' AND object_id = OBJECT_ID('DocumentHeaders'))
BEGIN
    DROP INDEX IX_DocumentHeaders_TenantId_IsDeleted ON DocumentHeaders;
    PRINT '  ✓ Dropped IX_DocumentHeaders_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageFacilities_TenantId_IsDeleted' AND object_id = OBJECT_ID('StorageFacilities'))
BEGIN
    DROP INDEX IX_StorageFacilities_TenantId_IsDeleted ON StorageFacilities;
    PRINT '  ✓ Dropped IX_StorageFacilities_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_TenantId_IsDeleted' AND object_id = OBJECT_ID('Products'))
BEGIN
    DROP INDEX IX_Products_TenantId_IsDeleted ON Products;
    PRINT '  ✓ Dropped IX_Products_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tenants_IsDeleted' AND object_id = OBJECT_ID('Tenants'))
BEGIN
    DROP INDEX IX_Tenants_IsDeleted ON Tenants;
    PRINT '  ✓ Dropped IX_Tenants_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_TenantId_IsDeleted' AND object_id = OBJECT_ID('Users'))
BEGIN
    DROP INDEX IX_Users_TenantId_IsDeleted ON Users;
    PRINT '  ✓ Dropped IX_Users_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_TenantId_IsDeleted' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    DROP INDEX IX_Notifications_TenantId_IsDeleted ON Notifications;
    PRINT '  ✓ Dropped IX_Notifications_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_TenantId_IsDeleted' AND object_id = OBJECT_ID('ChatMessages'))
BEGIN
    DROP INDEX IX_ChatMessages_TenantId_IsDeleted ON ChatMessages;
    PRINT '  ✓ Dropped IX_ChatMessages_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_TenantId_IsDeleted' AND object_id = OBJECT_ID('Events'))
BEGIN
    DROP INDEX IX_Events_TenantId_IsDeleted ON Events;
    PRINT '  ✓ Dropped IX_Events_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TableSessions_TenantId_IsDeleted' AND object_id = OBJECT_ID('TableSessions'))
BEGIN
    DROP INDEX IX_TableSessions_TenantId_IsDeleted ON TableSessions;
    PRINT '  ✓ Dropped IX_TableSessions_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleSessions_TenantId_IsDeleted' AND object_id = OBJECT_ID('SaleSessions'))
BEGIN
    DROP INDEX IX_SaleSessions_TenantId_IsDeleted ON SaleSessions;
    PRINT '  ✓ Dropped IX_SaleSessions_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentMethods_TenantId_IsDeleted' AND object_id = OBJECT_ID('PaymentMethods'))
BEGIN
    DROP INDEX IX_PaymentMethods_TenantId_IsDeleted ON PaymentMethods;
    PRINT '  ✓ Dropped IX_PaymentMethods_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NoteFlags_TenantId_IsDeleted' AND object_id = OBJECT_ID('NoteFlags'))
BEGIN
    DROP INDEX IX_NoteFlags_TenantId_IsDeleted ON NoteFlags;
    PRINT '  ✓ Dropped IX_NoteFlags_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UMs_TenantId_IsDeleted' AND object_id = OBJECT_ID('UMs'))
BEGIN
    DROP INDEX IX_UMs_TenantId_IsDeleted ON UMs;
    PRINT '  ✓ Dropped IX_UMs_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Models_TenantId_IsDeleted' AND object_id = OBJECT_ID('Models'))
BEGIN
    DROP INDEX IX_Models_TenantId_IsDeleted ON Models;
    PRINT '  ✓ Dropped IX_Models_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Brands_TenantId_IsDeleted' AND object_id = OBJECT_ID('Brands'))
BEGIN
    DROP INDEX IX_Brands_TenantId_IsDeleted ON Brands;
    PRINT '  ✓ Dropped IX_Brands_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageLocations_TenantId_IsDeleted' AND object_id = OBJECT_ID('StorageLocations'))
BEGIN
    DROP INDEX IX_StorageLocations_TenantId_IsDeleted ON StorageLocations;
    PRINT '  ✓ Dropped IX_StorageLocations_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Lots_TenantId_IsDeleted' AND object_id = OBJECT_ID('Lots'))
BEGIN
    DROP INDEX IX_Lots_TenantId_IsDeleted ON Lots;
    PRINT '  ✓ Dropped IX_Lots_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentCounters_TenantId_IsDeleted' AND object_id = OBJECT_ID('DocumentCounters'))
BEGIN
    DROP INDEX IX_DocumentCounters_TenantId_IsDeleted ON DocumentCounters;
    PRINT '  ✓ Dropped IX_DocumentCounters_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentTypes_TenantId_IsDeleted' AND object_id = OBJECT_ID('DocumentTypes'))
BEGIN
    DROP INDEX IX_DocumentTypes_TenantId_IsDeleted ON DocumentTypes;
    PRINT '  ✓ Dropped IX_DocumentTypes_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Banks_TenantId_IsDeleted' AND object_id = OBJECT_ID('Banks'))
BEGIN
    DROP INDEX IX_Banks_TenantId_IsDeleted ON Banks;
    PRINT '  ✓ Dropped IX_Banks_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTerms_TenantId_IsDeleted' AND object_id = OBJECT_ID('PaymentTerms'))
BEGIN
    DROP INDEX IX_PaymentTerms_TenantId_IsDeleted ON PaymentTerms;
    PRINT '  ✓ Dropped IX_PaymentTerms_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VatRates_TenantId_IsDeleted' AND object_id = OBJECT_ID('VatRates'))
BEGIN
    DROP INDEX IX_VatRates_TenantId_IsDeleted ON VatRates;
    PRINT '  ✓ Dropped IX_VatRates_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClassificationNodes_TenantId_IsDeleted' AND object_id = OBJECT_ID('ClassificationNodes'))
BEGIN
    DROP INDEX IX_ClassificationNodes_TenantId_IsDeleted ON ClassificationNodes;
    PRINT '  ✓ Dropped IX_ClassificationNodes_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Contacts_TenantId_IsDeleted' AND object_id = OBJECT_ID('Contacts'))
BEGIN
    DROP INDEX IX_Contacts_TenantId_IsDeleted ON Contacts;
    PRINT '  ✓ Dropped IX_Contacts_TenantId_IsDeleted';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Addresses_TenantId_IsDeleted' AND object_id = OBJECT_ID('Addresses'))
BEGIN
    DROP INDEX IX_Addresses_TenantId_IsDeleted ON Addresses;
    PRINT '  ✓ Dropped IX_Addresses_TenantId_IsDeleted';
END
GO

PRINT '';
PRINT '=============================================';
PRINT 'ROLLBACK COMPLETE!';
PRINT '=============================================';
PRINT 'All pagination performance indexes removed.';
GO
