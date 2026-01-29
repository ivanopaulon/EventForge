-- =============================================
-- Migration: Add Pagination Performance Indexes
-- Date: 2026-01-29
-- Description: FASE 6 Wave 4B - Comprehensive database indexes for pagination performance
--              Creates ~60 indexes across 5 categories to optimize query execution
--              and achieve 40-60% faster response times for cache MISS scenarios
-- =============================================

USE [EventData];
GO

-- =============================================
-- CATEGORY 1: Core Pagination Indexes (26)
-- =============================================
-- Pattern: Base filter for ALL paginated queries
-- Optimizes: WHERE TenantId = X AND IsDeleted = 0
-- =============================================

PRINT 'Creating Category 1: Core Pagination Indexes...';
GO

-- Core Management (8 tables)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Addresses_TenantId_IsDeleted' AND object_id = OBJECT_ID('Addresses'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Addresses_TenantId_IsDeleted
    ON Addresses (TenantId, IsDeleted)
    INCLUDE (Id, Street, City, ZipCode, Country)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Addresses_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Contacts_TenantId_IsDeleted' AND object_id = OBJECT_ID('Contacts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Contacts_TenantId_IsDeleted
    ON Contacts (TenantId, IsDeleted)
    INCLUDE (Id, FirstName, LastName, Email, Phone)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Contacts_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClassificationNodes_TenantId_IsDeleted' AND object_id = OBJECT_ID('ClassificationNodes'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClassificationNodes_TenantId_IsDeleted
    ON ClassificationNodes (TenantId, IsDeleted)
    INCLUDE (Id, Name, Code, ParentId)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_ClassificationNodes_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VatRates_TenantId_IsDeleted' AND object_id = OBJECT_ID('VatRates'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_VatRates_TenantId_IsDeleted
    ON VatRates (TenantId, IsDeleted)
    INCLUDE (Id, Name, Rate, IsDefault)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_VatRates_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTerms_TenantId_IsDeleted' AND object_id = OBJECT_ID('PaymentTerms'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PaymentTerms_TenantId_IsDeleted
    ON PaymentTerms (TenantId, IsDeleted)
    INCLUDE (Id, Name, DaysNet, IsDefault)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_PaymentTerms_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Banks_TenantId_IsDeleted' AND object_id = OBJECT_ID('Banks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Banks_TenantId_IsDeleted
    ON Banks (TenantId, IsDeleted)
    INCLUDE (Id, Name, BIC, Country)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Banks_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentTypes_TenantId_IsDeleted' AND object_id = OBJECT_ID('DocumentTypes'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentTypes_TenantId_IsDeleted
    ON DocumentTypes (TenantId, IsDeleted)
    INCLUDE (Id, Name, Code, Category)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_DocumentTypes_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentCounters_TenantId_IsDeleted' AND object_id = OBJECT_ID('DocumentCounters'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentCounters_TenantId_IsDeleted
    ON DocumentCounters (TenantId, IsDeleted)
    INCLUDE (Id, DocumentTypeId, CurrentValue, Prefix)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_DocumentCounters_TenantId_IsDeleted';
END
GO

-- Inventory Extended (4 tables)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Lots_TenantId_IsDeleted' AND object_id = OBJECT_ID('Lots'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Lots_TenantId_IsDeleted
    ON Lots (TenantId, IsDeleted)
    INCLUDE (Id, LotNumber, ProductId, Quantity, ExpirationDate)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Lots_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageLocations_TenantId_IsDeleted' AND object_id = OBJECT_ID('StorageLocations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_StorageLocations_TenantId_IsDeleted
    ON StorageLocations (TenantId, IsDeleted)
    INCLUDE (Id, Code, Zone, StorageFacilityId)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_StorageLocations_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Brands_TenantId_IsDeleted' AND object_id = OBJECT_ID('Brands'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Brands_TenantId_IsDeleted
    ON Brands (TenantId, IsDeleted)
    INCLUDE (Id, Name, Code, IsActive)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Brands_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Models_TenantId_IsDeleted' AND object_id = OBJECT_ID('Models'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Models_TenantId_IsDeleted
    ON Models (TenantId, IsDeleted)
    INCLUDE (Id, Name, Code, BrandId)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Models_TenantId_IsDeleted';
END
GO

-- Product & Payment (3 tables)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UMs_TenantId_IsDeleted' AND object_id = OBJECT_ID('UMs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_UMs_TenantId_IsDeleted
    ON UMs (TenantId, IsDeleted)
    INCLUDE (Id, Name, Code, UnitType)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_UMs_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NoteFlags_TenantId_IsDeleted' AND object_id = OBJECT_ID('NoteFlags'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_NoteFlags_TenantId_IsDeleted
    ON NoteFlags (TenantId, IsDeleted)
    INCLUDE (Id, Name, FlagType, Color)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_NoteFlags_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentMethods_TenantId_IsDeleted' AND object_id = OBJECT_ID('PaymentMethods'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PaymentMethods_TenantId_IsDeleted
    ON PaymentMethods (TenantId, IsDeleted)
    INCLUDE (Id, Name, Code, IsActive)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_PaymentMethods_TenantId_IsDeleted';
END
GO

-- POS & Sales (2 tables)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleSessions_TenantId_IsDeleted' AND object_id = OBJECT_ID('SaleSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleSessions_TenantId_IsDeleted
    ON SaleSessions (TenantId, IsDeleted)
    INCLUDE (Id, PosId, OperatorId, CreatedAt, ClosedAt, FinalTotal)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_SaleSessions_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TableSessions_TenantId_IsDeleted' AND object_id = OBJECT_ID('TableSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TableSessions_TenantId_IsDeleted
    ON TableSessions (TenantId, IsDeleted)
    INCLUDE (Id, TableNumber, Area, Capacity, Status, IsActive)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_TableSessions_TenantId_IsDeleted';
END
GO

-- Events & Communication (3 tables)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_TenantId_IsDeleted' AND object_id = OBJECT_ID('Events'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Events_TenantId_IsDeleted
    ON Events (TenantId, IsDeleted)
    INCLUDE (Id, Name, StartDate, CreatedBy)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Events_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_TenantId_IsDeleted' AND object_id = OBJECT_ID('ChatMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChatMessages_TenantId_IsDeleted
    ON ChatMessages (TenantId, IsDeleted)
    INCLUDE (Id, ChatThreadId, SenderId, Content, SentAt)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_ChatMessages_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_TenantId_IsDeleted' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_TenantId_IsDeleted
    ON Notifications (TenantId, IsDeleted)
    INCLUDE (Id, UserId, Type, Message, IsRead, CreatedAt)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Notifications_TenantId_IsDeleted';
END
GO

-- System & Admin (2 tables)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_TenantId_IsDeleted' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Users_TenantId_IsDeleted
    ON Users (TenantId, IsDeleted)
    INCLUDE (Id, FirstName, LastName, Email, IsActive)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Users_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tenants_IsDeleted' AND object_id = OBJECT_ID('Tenants'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tenants_IsDeleted
    ON Tenants (IsDeleted)
    INCLUDE (Id, Name, Code, IsActive)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Tenants_IsDeleted';
END
GO

-- Main Business Entities (4 tables)

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_TenantId_IsDeleted' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_TenantId_IsDeleted
    ON Products (TenantId, IsDeleted)
    INCLUDE (Id, Code, Name, DefaultPrice, CategoryNodeId, PreferredSupplierId, IsActive)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Products_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageFacilities_TenantId_IsDeleted' AND object_id = OBJECT_ID('StorageFacilities'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_StorageFacilities_TenantId_IsDeleted
    ON StorageFacilities (TenantId, IsDeleted)
    INCLUDE (Id, Code, Name, IsDefault)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_StorageFacilities_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentHeaders_TenantId_IsDeleted' AND object_id = OBJECT_ID('DocumentHeaders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentHeaders_TenantId_IsDeleted
    ON DocumentHeaders (TenantId, IsDeleted)
    INCLUDE (Id, Number, Date, BusinessPartyId, TotalGrossAmount, Status)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_DocumentHeaders_TenantId_IsDeleted';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BusinessParties_TenantId_IsDeleted' AND object_id = OBJECT_ID('BusinessParties'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BusinessParties_TenantId_IsDeleted
    ON BusinessParties (TenantId, IsDeleted)
    INCLUDE (Id, Code, Name, Type, IsActive)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_BusinessParties_TenantId_IsDeleted';
END
GO

PRINT 'Category 1 Complete: 26 Core Pagination Indexes created.';
GO

-- =============================================
-- CATEGORY 2: DateTime Sorting Indexes (8)
-- =============================================
-- Pattern: Optimize ORDER BY {DateField} DESC queries
-- Prevents: Sort operation in execution plan
-- =============================================

PRINT 'Creating Category 2: DateTime Sorting Indexes...';
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentHeaders_TenantId_DocumentDate' AND object_id = OBJECT_ID('DocumentHeaders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentHeaders_TenantId_DocumentDate
    ON DocumentHeaders (TenantId, Date DESC)
    INCLUDE (Id, Number, BusinessPartyId, TotalGrossAmount, Status)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_DocumentHeaders_TenantId_DocumentDate';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_TenantId_EventDate' AND object_id = OBJECT_ID('Events'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Events_TenantId_EventDate
    ON Events (TenantId, StartDate)
    INCLUDE (Id, Name, ShortDescription, CreatedBy)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Events_TenantId_EventDate';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleSessions_TenantId_CreatedAt' AND object_id = OBJECT_ID('SaleSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleSessions_TenantId_CreatedAt
    ON SaleSessions (TenantId, CreatedAt DESC)
    INCLUDE (Id, PosId, OperatorId, ClosedAt, FinalTotal)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_SaleSessions_TenantId_CreatedAt';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditTrails_TenantId_Timestamp' AND object_id = OBJECT_ID('AuditTrails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditTrails_TenantId_Timestamp
    ON AuditTrails (TenantId, Timestamp DESC)
    INCLUDE (Id, UserId, EntityType, Action, EntityId);
    PRINT '  ✓ Created IX_AuditTrails_TenantId_Timestamp';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LogEntries_TenantId_Timestamp' AND object_id = OBJECT_ID('LogEntries'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LogEntries_TenantId_Timestamp
    ON LogEntries (TenantId, Timestamp DESC)
    INCLUDE (Id, Level, Message, Exception);
    PRINT '  ✓ Created IX_LogEntries_TenantId_Timestamp';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_TenantId_SentAt' AND object_id = OBJECT_ID('ChatMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChatMessages_TenantId_SentAt
    ON ChatMessages (TenantId, SentAt DESC)
    INCLUDE (Id, ChatThreadId, SenderId, Content)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_ChatMessages_TenantId_SentAt';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_TenantId_CreatedAt' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_TenantId_CreatedAt
    ON Notifications (TenantId, CreatedAt DESC)
    INCLUDE (Id, UserId, Type, Message, IsRead)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Notifications_TenantId_CreatedAt';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EntityChangeLogs_TenantId_Timestamp' AND object_id = OBJECT_ID('EntityChangeLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_EntityChangeLogs_TenantId_Timestamp
    ON EntityChangeLogs (TenantId, Timestamp DESC)
    INCLUDE (Id, UserId, EntityType, Action, EntityId);
    PRINT '  ✓ Created IX_EntityChangeLogs_TenantId_Timestamp';
END
GO

PRINT 'Category 2 Complete: 8 DateTime Sorting Indexes created.';
GO

-- =============================================
-- CATEGORY 3: Foreign Key Indexes (15)
-- =============================================
-- Pattern: Optimize JOIN operations
-- Prevents: Nested loops with table scans
-- =============================================

PRINT 'Creating Category 3: Foreign Key Indexes...';
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CategoryId' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_CategoryId
    ON Products (CategoryNodeId)
    INCLUDE (Id, Name, DefaultPrice, Code)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Products_CategoryId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_SupplierId' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_SupplierId
    ON Products (PreferredSupplierId)
    INCLUDE (Id, Name, Code, DefaultPrice)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Products_SupplierId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_UnitOfMeasureId' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_UnitOfMeasureId
    ON Products (UnitOfMeasureId)
    INCLUDE (Id, Name, DefaultPrice)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Products_UnitOfMeasureId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_BrandId' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_BrandId
    ON Products (BrandId)
    INCLUDE (Id, Name, Code)
    WHERE IsDeleted = 0 AND BrandId IS NOT NULL;
    PRINT '  ✓ Created IX_Products_BrandId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Models_BrandId' AND object_id = OBJECT_ID('Models'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Models_BrandId
    ON Models (BrandId)
    INCLUDE (Id, Name, Code)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Models_BrandId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentRows_DocumentHeaderId' AND object_id = OBJECT_ID('DocumentRows'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentRows_DocumentHeaderId
    ON DocumentRows (DocumentHeaderId)
    INCLUDE (ProductId, Quantity, UnitPrice, Discount);
    PRINT '  ✓ Created IX_DocumentRows_DocumentHeaderId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentRows_ProductId' AND object_id = OBJECT_ID('DocumentRows'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentRows_ProductId
    ON DocumentRows (ProductId)
    INCLUDE (DocumentHeaderId, Quantity, UnitPrice);
    PRINT '  ✓ Created IX_DocumentRows_ProductId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_SaleSessionId' AND object_id = OBJECT_ID('SaleItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleItems_SaleSessionId
    ON SaleItems (SaleSessionId)
    INCLUDE (ProductId, Quantity, UnitPrice, Discount);
    PRINT '  ✓ Created IX_SaleItems_SaleSessionId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_ProductId' AND object_id = OBJECT_ID('SaleItems'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleItems_ProductId
    ON SaleItems (ProductId)
    INCLUDE (SaleSessionId, Quantity, UnitPrice);
    PRINT '  ✓ Created IX_SaleItems_ProductId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Lots_ProductId' AND object_id = OBJECT_ID('Lots'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Lots_ProductId
    ON Lots (ProductId)
    INCLUDE (LotNumber, Quantity, ExpirationDate)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Lots_ProductId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Lots_StorageLocationId' AND object_id = OBJECT_ID('Lots'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Lots_StorageLocationId
    ON Lots (StorageLocationId)
    INCLUDE (ProductId, Quantity, LotNumber)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Lots_StorageLocationId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StorageLocations_StorageFacilityId' AND object_id = OBJECT_ID('StorageLocations'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_StorageLocations_StorageFacilityId
    ON StorageLocations (StorageFacilityId)
    INCLUDE (Code, Zone)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_StorageLocations_StorageFacilityId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ClassificationNodes_ParentId' AND object_id = OBJECT_ID('ClassificationNodes'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClassificationNodes_ParentId
    ON ClassificationNodes (ParentId, TenantId)
    INCLUDE (Id, Name, Code)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_ClassificationNodes_ParentId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_ThreadId' AND object_id = OBJECT_ID('ChatMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChatMessages_ThreadId
    ON ChatMessages (ChatThreadId)
    INCLUDE (SenderId, Content, SentAt)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_ChatMessages_ThreadId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ChatMessages_SenderId' AND object_id = OBJECT_ID('ChatMessages'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChatMessages_SenderId
    ON ChatMessages (SenderId)
    INCLUDE (ChatThreadId, Content, SentAt)
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_ChatMessages_SenderId';
END
GO

PRINT 'Category 3 Complete: 15 Foreign Key Indexes created.';
GO

-- =============================================
-- CATEGORY 4: Composite Filter Indexes (8)
-- =============================================
-- Pattern: Optimize specific WHERE clause combinations
-- =============================================

PRINT 'Creating Category 4: Composite Filter Indexes...';
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleSessions_TenantId_OpenSessions' AND object_id = OBJECT_ID('SaleSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleSessions_TenantId_OpenSessions
    ON SaleSessions (TenantId, ClosedAt, CreatedAt DESC)
    INCLUDE (Id, PosId, OperatorId, FinalTotal)
    WHERE IsDeleted = 0 AND ClosedAt IS NULL;
    PRINT '  ✓ Created IX_SaleSessions_TenantId_OpenSessions';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TableSessions_TenantId_Available' AND object_id = OBJECT_ID('TableSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TableSessions_TenantId_Available
    ON TableSessions (TenantId, Status, Area)
    INCLUDE (Id, TableNumber, Capacity, IsActive)
    WHERE IsDeleted = 0 AND Status = 0; -- 0 = Available
    PRINT '  ✓ Created IX_TableSessions_TenantId_Available';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_TenantId_Active' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_TenantId_Active
    ON Products (TenantId, IsActive, Name)
    INCLUDE (Id, Code, DefaultPrice, CategoryNodeId, PreferredSupplierId)
    WHERE IsDeleted = 0 AND IsActive = 1;
    PRINT '  ✓ Created IX_Products_TenantId_Active';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditTrails_TenantId_EntityType_Timestamp' AND object_id = OBJECT_ID('AuditTrails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditTrails_TenantId_EntityType_Timestamp
    ON AuditTrails (TenantId, EntityType, Timestamp DESC)
    INCLUDE (Id, UserId, Action, EntityId);
    PRINT '  ✓ Created IX_AuditTrails_TenantId_EntityType_Timestamp';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditTrails_TenantId_UserId_Timestamp' AND object_id = OBJECT_ID('AuditTrails'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditTrails_TenantId_UserId_Timestamp
    ON AuditTrails (TenantId, UserId, Timestamp DESC)
    INCLUDE (Id, EntityType, Action, EntityId);
    PRINT '  ✓ Created IX_AuditTrails_TenantId_UserId_Timestamp';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LogEntries_TenantId_Level_Timestamp' AND object_id = OBJECT_ID('LogEntries'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LogEntries_TenantId_Level_Timestamp
    ON LogEntries (TenantId, Level, Timestamp DESC)
    INCLUDE (Id, Message, Exception);
    PRINT '  ✓ Created IX_LogEntries_TenantId_Level_Timestamp';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Notifications_TenantId_Unread' AND object_id = OBJECT_ID('Notifications'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Notifications_TenantId_Unread
    ON Notifications (TenantId, UserId, IsRead, CreatedAt DESC)
    INCLUDE (Id, Type, Message)
    WHERE IsDeleted = 0 AND IsRead = 0;
    PRINT '  ✓ Created IX_Notifications_TenantId_Unread';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserRoles_RoleName_UserId' AND object_id = OBJECT_ID('UserRoles'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_UserRoles_RoleName_UserId
    ON UserRoles (RoleName, UserId)
    INCLUDE (RoleId);
    PRINT '  ✓ Created IX_UserRoles_RoleName_UserId';
END
GO

PRINT 'Category 4 Complete: 8 Composite Filter Indexes created.';
GO

-- =============================================
-- CATEGORY 5: Covering Indexes (2)
-- =============================================
-- Pattern: Include ALL columns needed by SELECT
-- Eliminates: Key lookup operations
-- =============================================

PRINT 'Creating Category 5: Covering Indexes...';
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Covering' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_Covering
    ON Products (TenantId, IsDeleted, Name)
    INCLUDE (
        Id, Code, Description, Price, Cost, IsActive,
        CategoryId, SupplierId, BaseUnitOfMeasureId, BrandId, ModelId,
        StockQuantity, ReorderLevel, Barcode,
        CreatedAt, ModifiedAt, CreatedBy, ModifiedBy
    )
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_Products_Covering';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentHeaders_Covering' AND object_id = OBJECT_ID('DocumentHeaders'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentHeaders_Covering
    ON DocumentHeaders (TenantId, IsDeleted, Date DESC)
    INCLUDE (
        Id, Number, Series, BusinessPartyId, TotalGrossAmount, TotalNetAmount, VatAmount, Status,
        CustomerName, ShippingNotes, TrackingNumber,
        CreatedAt, ModifiedAt, CreatedBy, ModifiedBy
    )
    WHERE IsDeleted = 0;
    PRINT '  ✓ Created IX_DocumentHeaders_Covering';
END
GO

PRINT 'Category 5 Complete: 2 Covering Indexes created.';
GO

-- =============================================
-- SUMMARY
-- =============================================

PRINT '';
PRINT '=============================================';
PRINT 'MIGRATION COMPLETE!';
PRINT '=============================================';
PRINT '';
PRINT 'Total Indexes Created: ~60';
PRINT '  - Category 1 (Core Pagination): 26 indexes';
PRINT '  - Category 2 (DateTime Sorting): 8 indexes';
PRINT '  - Category 3 (Foreign Keys): 15 indexes';
PRINT '  - Category 4 (Composite Filters): 8 indexes';
PRINT '  - Category 5 (Covering Indexes): 2 indexes';
PRINT '';
PRINT 'Expected Performance Improvements:';
PRINT '  - Cache MISS scenarios: 40-60% faster';
PRINT '  - Table scans → Index seeks';
PRINT '  - Reduced I/O operations: 90-99%';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Monitor query execution plans';
PRINT '  2. Check for missing index DMVs';
PRINT '  3. Monitor index fragmentation';
PRINT '  4. Validate performance benchmarks';
PRINT '';
PRINT '=============================================';
GO
