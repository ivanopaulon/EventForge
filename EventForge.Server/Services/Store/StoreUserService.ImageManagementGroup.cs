using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Store;


namespace EventForge.Server.Services.Store;

public partial class StoreUserService
{
    public async Task<StoreUserGroupDto?> UploadStoreUserGroupLogoAsync(Guid groupId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        var group = await context.StoreUserGroups
            .Include(g => g.LogoDocument)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted && g.TenantId == currentTenantId.Value, cancellationToken);

        if (group is null)
        {
            logger.LogWarning("Store user group {GroupId} not found for logo upload in tenant {TenantId}.", groupId, currentTenantId.Value);
            return null;
        }

        // Generate a unique filename
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"storegroup_{groupId}_{Guid.NewGuid()}{extension}";

        // Save to wwwroot/images/storegroups
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "storegroups");
        _ = Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, fileName);
        var storageKey = $"/images/storegroups/{fileName}";

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // Create or update DocumentReference
        var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
        {
            TenantId = currentTenantId.Value,
            OwnerId = groupId,
            OwnerType = "StoreUserGroup",
            FileName = file.FileName,
            Type = Prym.DTOs.Common.DocumentReferenceType.ProfilePhoto,
            SubType = Prym.DTOs.Common.DocumentReferenceSubType.None,
            MimeType = file.ContentType,
            StorageKey = storageKey,
            Url = storageKey,
            FileSizeBytes = file.Length,
            Title = $"Store User Group {group.Name} Logo",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        // If group already has a logo, delete the old one first
        if (group.LogoDocumentId.HasValue)
        {
            var oldDocument = await context.DocumentReferences
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == group.LogoDocumentId.Value, cancellationToken);

            if (oldDocument is not null)
            {
                // Delete old physical file
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldDocument.StorageKey.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }

                _ = context.DocumentReferences.Remove(oldDocument);
            }
        }

        _ = context.DocumentReferences.Add(documentReference);
        _ = await context.SaveChangesAsync(cancellationToken);

        // Update group with new DocumentReference ID
        group.LogoDocumentId = documentReference.Id;
        group.ModifiedAt = DateTime.UtcNow;
        group.ModifiedBy = "System";

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store user group {GroupId} logo uploaded successfully as DocumentReference {DocumentId}.", groupId, documentReference.Id);

        // Reload to get the document reference
        group.LogoDocument = documentReference;

        // Get counts for DTO
        var cashierCount = await context.StoreUsers.AsNoTracking().CountAsync(su => su.CashierGroupId == groupId && !su.IsDeleted, cancellationToken);
        var privilegeCount = await context.StoreUserPrivileges
            .AsNoTracking()
            .CountAsync(sup => sup.Groups.Any(g => g.Id == groupId) && !sup.IsDeleted && sup.TenantId == currentTenantId.Value, cancellationToken);

        return MapToStoreUserGroupDto(group, cashierCount, privilegeCount);
    }

    public async Task<Prym.DTOs.Teams.DocumentReferenceDto?> GetStoreUserGroupLogoDocumentAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        var group = await context.StoreUserGroups
            .Include(g => g.LogoDocument)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted && g.TenantId == currentTenantId.Value, cancellationToken);

        if (group?.LogoDocument is null)
        {
            logger.LogWarning("Store user group {GroupId} not found or has no logo in tenant {TenantId}.", groupId, currentTenantId.Value);
            return null;
        }

        return MapToDocumentReferenceDto(group.LogoDocument);
    }

    public async Task<bool> DeleteStoreUserGroupLogoAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user group operations.");
        }

        var group = await context.StoreUserGroups
            .AsNoTracking()
            .Include(g => g.LogoDocument)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted && g.TenantId == currentTenantId.Value, cancellationToken);

        if (group?.LogoDocument is null)
        {
            logger.LogWarning("Store user group {GroupId} not found or has no logo to delete in tenant {TenantId}.", groupId, currentTenantId.Value);
            return false;
        }

        // Delete physical file
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", group.LogoDocument.StorageKey.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Remove DocumentReference
        _ = context.DocumentReferences.Remove(group.LogoDocument);

        // Update group
        group.LogoDocumentId = null;
        group.ModifiedAt = DateTime.UtcNow;
        group.ModifiedBy = "System";

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store user group {GroupId} logo deleted successfully.", groupId);
        return true;
    }

}
