using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Store;


namespace EventForge.Server.Services.Store;

public partial class StoreUserService
{
    public async Task<StoreUserDto?> UploadStoreUserPhotoAsync(Guid storeUserId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var storeUser = await context.StoreUsers
            .Include(su => su.PhotoDocument)
            .Include(su => su.CashierGroup)
            .FirstOrDefaultAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);

        if (storeUser is null)
        {
            logger.LogWarning("Store user {StoreUserId} not found for photo upload in tenant {TenantId}.", storeUserId, currentTenantId.Value);
            return null;
        }

        // GDPR Compliance: Check photo consent
        if (!storeUser.PhotoConsent)
        {
            throw new InvalidOperationException("Photo upload requires explicit user consent (GDPR). PhotoConsent must be true.");
        }

        // Generate a unique filename
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"storeuser_{storeUserId}_{Guid.NewGuid()}{extension}";

        // Save to wwwroot/images/storeusers
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "storeusers");
        _ = Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, fileName);
        var storageKey = $"/images/storeusers/{fileName}";

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // Create or update DocumentReference
        var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
        {
            TenantId = currentTenantId.Value,
            OwnerId = storeUserId,
            OwnerType = "StoreUser",
            FileName = file.FileName,
            Type = Prym.DTOs.Common.DocumentReferenceType.ProfilePhoto,
            SubType = Prym.DTOs.Common.DocumentReferenceSubType.None,
            MimeType = file.ContentType,
            StorageKey = storageKey,
            Url = storageKey,
            FileSizeBytes = file.Length,
            Title = $"Store User {storeUser.Name} Photo",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        // If store user already has a photo, delete the old one first
        if (storeUser.PhotoDocumentId.HasValue)
        {
            var oldDocument = await context.DocumentReferences
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == storeUser.PhotoDocumentId.Value, cancellationToken);

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

        // Update store user with new DocumentReference ID
        storeUser.PhotoDocumentId = documentReference.Id;
        storeUser.ModifiedAt = DateTime.UtcNow;
        storeUser.ModifiedBy = "System";

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store user {StoreUserId} photo uploaded successfully as DocumentReference {DocumentId}.", storeUserId, documentReference.Id);

        // Reload to get the document reference
        storeUser.PhotoDocument = documentReference;
        return MapToStoreUserDto(storeUser);
    }

    public async Task<Prym.DTOs.Teams.DocumentReferenceDto?> GetStoreUserPhotoDocumentAsync(Guid storeUserId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var storeUser = await context.StoreUsers
            .Include(su => su.PhotoDocument)
            .FirstOrDefaultAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);

        if (storeUser?.PhotoDocument is null)
        {
            logger.LogWarning("Store user {StoreUserId} not found or has no photo in tenant {TenantId}.", storeUserId, currentTenantId.Value);
            return null;
        }

        return MapToDocumentReferenceDto(storeUser.PhotoDocument);
    }

    public async Task<bool> DeleteStoreUserPhotoAsync(Guid storeUserId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store user operations.");
        }

        var storeUser = await context.StoreUsers
            .AsNoTracking()
            .Include(su => su.PhotoDocument)
            .FirstOrDefaultAsync(su => su.Id == storeUserId && !su.IsDeleted && su.TenantId == currentTenantId.Value, cancellationToken);

        if (storeUser?.PhotoDocument is null)
        {
            logger.LogWarning("Store user {StoreUserId} not found or has no photo to delete in tenant {TenantId}.", storeUserId, currentTenantId.Value);
            return false;
        }

        // Delete physical file
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storeUser.PhotoDocument.StorageKey.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Remove DocumentReference
        _ = context.DocumentReferences.Remove(storeUser.PhotoDocument);

        // Update store user
        storeUser.PhotoDocumentId = null;
        storeUser.ModifiedAt = DateTime.UtcNow;
        storeUser.ModifiedBy = "System";

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store user {StoreUserId} photo deleted successfully.", storeUserId);
        return true;
    }

}
