# EventForge Notifications & Chat Data Model

## Overview

This document describes the database schema for the **Step 2** implementation of EventForge's SignalR Notifications & Chat system with multi-tenant support. The data model supports real-time notifications and chat functionality with comprehensive multi-tenant isolation, localization, and audit tracking.

## Entity Architecture

All entities inherit from `AuditableEntity`, providing:
- **Multi-tenancy**: Automatic `TenantId` isolation
- **Audit trails**: Created/Modified/Deleted tracking with user attribution
- **Soft deletion**: Logical deletion with `IsDeleted` flag
- **Concurrency**: Optimistic locking with `RowVersion`

## Notifications Schema

### Notification
Core notification entity supporting broadcast and targeted messaging.

**Key Properties:**
- `Id` (Guid): Primary key
- `TenantId` (Guid): Multi-tenant isolation (inherited)
- `SenderId` (Guid?): Optional sender user ID
- `Type` (NotificationTypes): System, Event, User, Security, Audit, Marketing
- `Priority` (NotificationPriority): Low, Normal, High, Critical
- `Status` (NotificationStatus): Pending, Sent, Delivered, Read, Acknowledged, Silenced, Archived, Expired
- `Title` (string, max 200): Notification title or localization key
- `Message` (string, max 1000): Message body or localization key
- `ActionUrl` (string?, max 500): Optional clickable action URL
- `IconUrl` (string?, max 500): Optional icon/image URL
- `PayloadLocale` (string?, max 10): Language/locale (e.g., "en-US", "it-IT")
- `LocalizationParamsJson` (string?): JSON parameters for dynamic content
- `ExpiresAt` (DateTime?): Optional expiration timestamp
- `IsArchived` (bool): Archive status
- `MetadataJson` (string?): Additional extensibility metadata (JSON)

**Indexes:**
- `TenantId`, `Status`, `Type`, `Priority`, `ExpiresAt`, `CreatedAt`, `IsArchived`

### NotificationRecipient
Per-user notification delivery and status tracking.

**Key Properties:**
- `NotificationId` (Guid): Foreign key to Notification
- `RecipientUserId` (Guid): Target user ID
- `Status` (NotificationStatus): Per-recipient status
- `ReadAt`, `AcknowledgedAt`, `SilencedAt`, `ArchivedAt` (DateTime?): Status timestamps
- `IsArchived` (bool): Per-recipient archive flag

**Indexes:**
- `TenantId`, `NotificationId`, `RecipientUserId`, `Status`, `ReadAt`
- **Unique Constraint**: `(NotificationId, RecipientUserId)`

## Chat Schema

### ChatThread
Chat conversation/thread container supporting DM, Group, and Channel types.

**Key Properties:**
- `Type` (ChatType): DirectMessage, Group, Channel
- `Name` (string?, max 100): Optional name (auto-generated for DMs)
- `Description` (string?, max 500): Optional description for groups/channels
- `IsPrivate` (bool): Visibility control
- `PreferredLocale` (string?, max 10): Chat language preference
- `UpdatedAt` (DateTime): Last activity timestamp

**Indexes:**
- `TenantId`, `Type`, `IsPrivate`, `CreatedAt`, `UpdatedAt`

### ChatMember
Chat thread membership with role management.

**Key Properties:**
- `ChatThreadId` (Guid): Foreign key to ChatThread
- `UserId` (Guid): Member user ID
- `Role` (ChatMemberRole): Member, Admin, Moderator, Owner
- `JoinedAt` (DateTime): Membership start timestamp
- `LastSeenAt` (DateTime?): Last activity in chat
- `IsOnline` (bool): Real-time online status
- `IsMuted` (bool): User-specific mute setting

**Indexes:**
- `TenantId`, `ChatThreadId`, `UserId`, `Role`, `JoinedAt`, `LastSeenAt`
- **Unique Constraint**: `(ChatThreadId, UserId)`

### ChatMessage
Individual chat messages with threading and media support.

**Key Properties:**
- `ChatThreadId` (Guid): Foreign key to ChatThread
- `SenderId` (Guid): Message sender user ID
- `Content` (string?, max 4000): Message text content
- `ReplyToMessageId` (Guid?): Optional message threading
- `Status` (MessageStatus): Pending, Sent, Delivered, Read, Failed, Deleted
- `SentAt` (DateTime): Send timestamp
- `DeliveredAt`, `ReadAt`, `EditedAt` (DateTime?): Status timestamps
- `IsEdited` (bool): Edit flag
- `Locale` (string?, max 10): Message language
- `MetadataJson` (string?): Additional message metadata (JSON)

**Indexes:**
- `TenantId`, `ChatThreadId`, `SenderId`, `Status`, `SentAt`, `ReplyToMessageId`, `IsDeleted`

### MessageAttachment
File and media attachments for chat messages.

**Key Properties:**
- `MessageId` (Guid): Foreign key to ChatMessage
- `FileName` (string, max 255): Server-side filename
- `OriginalFileName` (string?, max 255): User-uploaded filename
- `FileSize` (long): File size in bytes
- `ContentType` (string, max 100): MIME type
- `MediaType` (MediaType): Document, Image, Video, Audio, Archive, Other
- `FileUrl`, `ThumbnailUrl` (string?, max 500): Access URLs
- `UploadedAt` (DateTime): Upload timestamp
- `UploadedBy` (Guid): Uploader user ID
- `MediaMetadataJson` (string?): Media properties (dimensions, duration, etc.)

**Indexes:**
- `TenantId`, `MessageId`, `MediaType`, `UploadedAt`, `UploadedBy`

### MessageReadReceipt
Read confirmation tracking for group chat messages.

**Key Properties:**
- `MessageId` (Guid): Foreign key to ChatMessage
- `UserId` (Guid): User who read the message
- `ReadAt` (DateTime): Read timestamp

**Indexes:**
- `TenantId`, `MessageId`, `UserId`, `ReadAt`
- **Unique Constraint**: `(MessageId, UserId)`

## Multi-Tenant Features

### Tenant Isolation
- All entities include `TenantId` with automatic indexing
- Global query filters ensure tenant-specific data access
- Audit trails maintain tenant context

### Indexing Strategy
- **Primary indexes**: `TenantId` on all tables for isolation
- **Status indexes**: Notification/message status for filtering
- **Type indexes**: Notification types and chat types for categorization
- **Temporal indexes**: Expiry, archiving, and timestamp-based queries
- **Language indexes**: Locale-based filtering for localization

### Data Relationships
- **Cascade deletes**: Recipients, members, attachments, read receipts
- **Restricted deletes**: Messages → threads (preserve history)
- **Soft deletes**: All entities support logical deletion
- **Set null**: Message replies (preserve thread structure)

## Migration Details

**Migration**: `20250801083821_AddNotificationsAndChatModels`

Creates all tables with:
- Primary keys and foreign key constraints
- Comprehensive indexing for performance
- Unique constraints for data integrity
- Proper column types and constraints
- JSON columns for flexible metadata storage

## Next Steps

This data model supports the foundation for:
1. **Step 3**: SignalR notification and chat services
2. **Step 4**: MudBlazor UI components
3. **Step 5**: Security, audit, rate limiting, and testing

The schema is designed for scalability, performance, and maintainability in a multi-tenant SaaS environment.