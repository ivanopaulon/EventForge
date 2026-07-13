using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Store;


namespace EventForge.Server.Services.Store;

public partial class StoreUserService
{
    public async Task<StorePosDto?> UploadStorePosImageAsync(Guid storePosId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store POS operations.");
        }

        var storePos = await context.StorePoses
            .Include(sp => sp.ImageDocument)
            .Include(sp => sp.CashierGroup)
            .Include(sp => sp.DefaultFiscalPrinter)
            .Include(sp => sp.DefaultPaymentTerminal)
            .FirstOrDefaultAsync(sp => sp.Id == storePosId && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

        if (storePos is null)
        {
            logger.LogWarning("Store POS {StorePosId} not found for image upload in tenant {TenantId}.", storePosId, currentTenantId.Value);
            return null;
        }

        // Generate a unique filename
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"storepos_{storePosId}_{Guid.NewGuid()}{extension}";

        // Save to wwwroot/images/storepos
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "storepos");
        _ = Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, fileName);
        var storageKey = $"/images/storepos/{fileName}";

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // Create or update DocumentReference
        var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
        {
            TenantId = currentTenantId.Value,
            OwnerId = storePosId,
            OwnerType = "StorePos",
            FileName = file.FileName,
            Type = Prym.DTOs.Common.DocumentReferenceType.ProfilePhoto,
            SubType = Prym.DTOs.Common.DocumentReferenceSubType.None,
            MimeType = file.ContentType,
            StorageKey = storageKey,
            Url = storageKey,
            FileSizeBytes = file.Length,
            Title = $"Store POS {storePos.Name} Image",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        // If store POS already has an image, delete the old one first
        if (storePos.ImageDocumentId.HasValue)
        {
            var oldDocument = await context.DocumentReferences
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == storePos.ImageDocumentId.Value, cancellationToken);

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

        // Update store POS with new DocumentReference ID
        storePos.ImageDocumentId = documentReference.Id;
        storePos.ModifiedAt = DateTime.UtcNow;
        storePos.ModifiedBy = "System";

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store POS {StorePosId} image uploaded successfully as DocumentReference {DocumentId}.", storePosId, documentReference.Id);

        // Reload to get the document reference
        storePos.ImageDocument = documentReference;
        return MapToStorePosDto(storePos);
    }

    public async Task<Prym.DTOs.Teams.DocumentReferenceDto?> GetStorePosImageDocumentAsync(Guid storePosId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store POS operations.");
        }

        var storePos = await context.StorePoses
            .Include(sp => sp.ImageDocument)
            .FirstOrDefaultAsync(sp => sp.Id == storePosId && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

        if (storePos?.ImageDocument is null)
        {
            logger.LogWarning("Store POS {StorePosId} not found or has no image in tenant {TenantId}.", storePosId, currentTenantId.Value);
            return null;
        }

        return MapToDocumentReferenceDto(storePos.ImageDocument);
    }

    public async Task<bool> DeleteStorePosImageAsync(Guid storePosId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for store POS operations.");
        }

        var storePos = await context.StorePoses
            .AsNoTracking()
            .Include(sp => sp.ImageDocument)
            .FirstOrDefaultAsync(sp => sp.Id == storePosId && !sp.IsDeleted && sp.TenantId == currentTenantId.Value, cancellationToken);

        if (storePos?.ImageDocument is null)
        {
            logger.LogWarning("Store POS {StorePosId} not found or has no image to delete in tenant {TenantId}.", storePosId, currentTenantId.Value);
            return false;
        }

        // Delete physical file
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storePos.ImageDocument.StorageKey.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Remove DocumentReference
        _ = context.DocumentReferences.Remove(storePos.ImageDocument);

        // Update store POS
        storePos.ImageDocumentId = null;
        storePos.ModifiedAt = DateTime.UtcNow;
        storePos.ModifiedBy = "System";

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Store POS {StorePosId} image deleted successfully.", storePosId);
        return true;
    }
}
