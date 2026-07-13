using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Store;

namespace EventForge.Server.Services.Store;

/// <summary>
/// Service implementation for managing store users, groups, and privileges.
/// </summary>
public partial class StoreUserService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    IPasswordService passwordService,
    ITenantContext tenantContext,
    ILogger<StoreUserService> logger) : IStoreUserService
{

    #region Helper Methods

    public async Task<bool> StoreUserExistsAsync(Guid storeUserId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        return await context.StoreUsers
            .AsNoTracking()
            .AnyAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);
    }

    public async Task<bool> ValidatePinAsync(Guid storeUserId, string pin, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var quickPinHash = await context.StoreUsers
            .AsNoTracking()
            .Where(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value)
            .Select(su => su.QuickPinHash)
            .FirstOrDefaultAsync(cancellationToken);

        return VerifyQuickPin(pin, quickPinHash);
    }

    public async Task SetPinAsync(Guid storeUserId, string pin, string currentUser, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var storeUser = await context.StoreUsers
            .FirstOrDefaultAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);

        if (storeUser is null)
        {
            throw new InvalidOperationException($"Store user with ID {storeUserId} not found.");
        }

        var originalValues = context.Entry(storeUser).CurrentValues.Clone();
        var originalStoreUser = (StoreUser)originalValues.ToObject();
        var (hash, salt) = passwordService.HashPassword(pin);

        storeUser.QuickPinHash = $"{hash}|{salt}";
        storeUser.ModifiedAt = DateTime.UtcNow;
        storeUser.ModifiedBy = currentUser;

        await context.SaveChangesAsync(cancellationToken);
        _ = await auditLogService.TrackEntityChangesAsync(storeUser, "Update", currentUser, originalStoreUser, cancellationToken);

        logger.LogInformation("Quick PIN updated for store user {StoreUserId} by {User}.", storeUserId, currentUser);
    }

    public async Task<IEnumerable<StoreUserDto>> GetStoreUsersWithBirthdayAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue) return Enumerable.Empty<StoreUserDto>();

        var storeUsers = await context.StoreUsers
            .AsNoTracking()
            .Where(su => !su.IsDeleted && su.DateOfBirth.HasValue && su.TenantId == currentTenantId.Value)
            .OrderBy(su => su.Name)
            .ToListAsync(cancellationToken);

        return storeUsers.Select(MapToStoreUserDto);
    }

    public async Task<bool> StoreUserGroupExistsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        return await context.StoreUserGroups
            .AsNoTracking()
            .AnyAsync(sug => sug.Id == groupId && !sug.IsDeleted && sug.TenantId == currentTenantId.Value, cancellationToken);
    }

    private static StoreUserDto MapToStoreUserDto(StoreUser storeUser)
    {
        return new StoreUserDto
        {
            Id = storeUser.Id,
            Name = storeUser.Name,
            Username = storeUser.Username,
            Email = storeUser.Email,
            Role = storeUser.Role,
            Status = (Prym.DTOs.Common.CashierStatus)storeUser.Status,
            LastLoginAt = storeUser.LastLoginAt,
            Notes = storeUser.Notes,
            CashierGroupId = storeUser.CashierGroupId,
            CashierGroupName = storeUser.CashierGroup?.Name,
            // Issue #315: Image Management & Extended Fields
            PhotoDocumentId = storeUser.PhotoDocumentId,
            PhotoUrl = storeUser.PhotoDocument?.StorageKey,
            PhotoThumbnailUrl = storeUser.PhotoDocument?.ThumbnailStorageKey,
            ImageDocumentId = storeUser.PhotoDocumentId,
            ImageUrl = storeUser.PhotoDocument?.Url ?? storeUser.PhotoDocument?.StorageKey,
            ImageThumbnailUrl = storeUser.PhotoDocument?.ThumbnailStorageKey,
            PhotoConsent = storeUser.PhotoConsent,
            DateOfBirth = storeUser.DateOfBirth,
            PhotoConsentAt = storeUser.PhotoConsentAt,
            PhoneNumber = storeUser.PhoneNumber,
            LastPasswordChangedAt = storeUser.LastPasswordChangedAt,
            TwoFactorEnabled = storeUser.TwoFactorEnabled,
            ExternalId = storeUser.ExternalId,
            IsOnShift = storeUser.IsOnShift,
            ShiftId = storeUser.ShiftId,
            CreatedAt = storeUser.CreatedAt,
            CreatedBy = storeUser.CreatedBy,
            ModifiedAt = storeUser.ModifiedAt,
            ModifiedBy = storeUser.ModifiedBy
        };
    }

    private static StoreUserGroupDto MapToStoreUserGroupDto(StoreUserGroup storeUserGroup, int cashierCount, int privilegeCount)
    {
        return new StoreUserGroupDto
        {
            Id = storeUserGroup.Id,
            Code = storeUserGroup.Code,
            Name = storeUserGroup.Name,
            Description = storeUserGroup.Description,
            Status = (Prym.DTOs.Common.CashierGroupStatus)storeUserGroup.Status,
            CashierCount = cashierCount,
            PrivilegeCount = privilegeCount,
            // Issue #315: Image Management & Branding Fields
            LogoDocumentId = storeUserGroup.LogoDocumentId,
            LogoUrl = storeUserGroup.LogoDocument?.StorageKey,
            LogoThumbnailUrl = storeUserGroup.LogoDocument?.ThumbnailStorageKey,
            ImageDocumentId = storeUserGroup.LogoDocumentId,
            ImageUrl = storeUserGroup.LogoDocument?.Url ?? storeUserGroup.LogoDocument?.StorageKey,
            ImageThumbnailUrl = storeUserGroup.LogoDocument?.ThumbnailStorageKey,
            ColorHex = storeUserGroup.ColorHex,
            IsSystemGroup = storeUserGroup.IsSystemGroup,
            IsDefault = storeUserGroup.IsDefault,
            CreatedAt = storeUserGroup.CreatedAt,
            CreatedBy = storeUserGroup.CreatedBy,
            ModifiedAt = storeUserGroup.ModifiedAt,
            ModifiedBy = storeUserGroup.ModifiedBy
        };
    }

    private static StoreUserPrivilegeDto MapToStoreUserPrivilegeDto(StoreUserPrivilege storeUserPrivilege, int groupCount)
    {
        return new StoreUserPrivilegeDto
        {
            Id = storeUserPrivilege.Id,
            Code = storeUserPrivilege.Code,
            Name = storeUserPrivilege.Name,
            Category = storeUserPrivilege.Category,
            Description = storeUserPrivilege.Description,
            Status = (Prym.DTOs.Common.CashierPrivilegeStatus)storeUserPrivilege.Status,
            SortOrder = storeUserPrivilege.SortOrder,
            GroupCount = groupCount,
            ImageDocumentId = storeUserPrivilege.ImageDocumentId,
            ImageUrl = storeUserPrivilege.ImageDocument?.Url ?? storeUserPrivilege.ImageDocument?.StorageKey,
            // Issue #315: Permission System Fields
            IsSystemPrivilege = storeUserPrivilege.IsSystemPrivilege,
            DefaultAssigned = storeUserPrivilege.DefaultAssigned,
            Resource = storeUserPrivilege.Resource,
            Action = storeUserPrivilege.Action,
            PermissionKey = storeUserPrivilege.PermissionKey,
            CreatedAt = storeUserPrivilege.CreatedAt,
            CreatedBy = storeUserPrivilege.CreatedBy,
            ModifiedAt = storeUserPrivilege.ModifiedAt,
            ModifiedBy = storeUserPrivilege.ModifiedBy
        };
    }

    private bool VerifyQuickPin(string pin, string? storedQuickPinHash)
    {
        if (string.IsNullOrWhiteSpace(pin) || string.IsNullOrWhiteSpace(storedQuickPinHash))
        {
            return false;
        }

        var parts = storedQuickPinHash.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            logger.LogWarning("Quick PIN hash for a store user is not in the expected format.");
            return false;
        }

        return passwordService.VerifyPassword(pin, parts[0], parts[1]);
    }

    #endregion

    #region Image Management Methods - Issue #315

    private static Prym.DTOs.Teams.DocumentReferenceDto MapToDocumentReferenceDto(EventForge.Server.Data.Entities.Teams.DocumentReference documentReference)
    {
        return new Prym.DTOs.Teams.DocumentReferenceDto
        {
            Id = documentReference.Id,
            OwnerId = documentReference.OwnerId,
            OwnerType = documentReference.OwnerType,
            FileName = documentReference.FileName,
            Type = documentReference.Type,
            SubType = documentReference.SubType,
            MimeType = documentReference.MimeType,
            StorageKey = documentReference.StorageKey,
            Url = documentReference.Url,
            ThumbnailStorageKey = documentReference.ThumbnailStorageKey,
            Expiry = documentReference.Expiry,
            FileSizeBytes = documentReference.FileSizeBytes,
            Title = documentReference.Title,
            Notes = documentReference.Notes,
            CreatedAt = documentReference.CreatedAt,
            CreatedBy = documentReference.CreatedBy,
            ModifiedAt = documentReference.ModifiedAt,
            ModifiedBy = documentReference.ModifiedBy
        };
    }

    private StorePosDto MapToStorePosDto(EventForge.Server.Data.Entities.Store.StorePos storePos)
    {
        return new StorePosDto
        {
            Id = storePos.Id,
            Name = storePos.Name,
            Description = storePos.Description,
            Status = (Prym.DTOs.Common.CashRegisterStatus)storePos.Status,
            Location = storePos.Location,
            LastOpenedAt = storePos.LastOpenedAt,
            Notes = storePos.Notes,
            // Issue #315: Image Management & Extended Fields
            ImageDocumentId = storePos.ImageDocumentId,
            ImageUrl = storePos.ImageDocument?.Url,
            ImageThumbnailUrl = storePos.ImageDocument?.ThumbnailStorageKey,
            TerminalIdentifier = storePos.TerminalIdentifier,
            IPAddress = storePos.IPAddress,
            IsOnline = storePos.IsOnline,
            LastSyncAt = storePos.LastSyncAt,
            LocationLatitude = storePos.LocationLatitude,
            LocationLongitude = storePos.LocationLongitude,
            CurrencyCode = storePos.CurrencyCode,
            TimeZone = storePos.TimeZone,
            DefaultFiscalPrinterId = storePos.DefaultFiscalPrinterId,
            DefaultFiscalPrinterName = storePos.DefaultFiscalPrinter?.Name,
            DefaultPaymentTerminalId = storePos.DefaultPaymentTerminalId,
            DefaultPaymentTerminalName = storePos.DefaultPaymentTerminal?.Name,
            CashierGroupId = storePos.CashierGroupId,
            CashierGroupName = storePos.CashierGroup?.Name,
            CreatedAt = storePos.CreatedAt,
            CreatedBy = storePos.CreatedBy,
            ModifiedAt = storePos.ModifiedAt,
            ModifiedBy = storePos.ModifiedBy
        };
    }

    #endregion

}
