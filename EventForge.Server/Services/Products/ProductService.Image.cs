using EventForge.Server.Services.CodeGeneration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Products;
using EntityProductCodeStatus = EventForge.Server.Data.Entities.Products.ProductCodeStatus;
using EntityProductStatus = EventForge.Server.Data.Entities.Products.ProductStatus;
using EntityProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;


namespace EventForge.Server.Services.Products;

public partial class ProductService
{
    public async Task<ProductDto?> UploadProductImageAsync(Guid productId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        var product = await context.Products
            .Include(p => p.Codes.Where(c => !c.IsDeleted))
            .Include(p => p.Units.Where(u => !u.IsDeleted))
            .Include(p => p.BundleItems.Where(bi => !bi.IsDeleted))
            .Include(p => p.ImageDocument)
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

        if (product is null)
        {
            logger.LogWarning("Product {ProductId} not found for image upload in tenant {TenantId}.", productId, currentTenantId.Value);
            return null;
        }

        // Generate a unique filename
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"product_{productId}_{Guid.NewGuid()}{extension}";

        // Save to wwwroot/images/products (in production, use cloud storage)
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
        _ = Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, fileName);
        var storageKey = $"/images/products/{fileName}";

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // Create or update DocumentReference
        var documentReference = new EventForge.Server.Data.Entities.Teams.DocumentReference
        {
            TenantId = currentTenantId.Value,
            OwnerId = productId,
            OwnerType = "Product",
            FileName = file.FileName,
            Type = Prym.DTOs.Common.DocumentReferenceType.ProfilePhoto,
            SubType = Prym.DTOs.Common.DocumentReferenceSubType.None,
            MimeType = file.ContentType,
            StorageKey = storageKey,
            Url = storageKey,
            ThumbnailStorageKey = storageKey, // <- ensure thumbnail key is set
            FileSizeBytes = file.Length,
            Title = $"Product {product.Name} Image",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System"
        };

        // If product already has an image, delete the old one first
        if (product.ImageDocumentId.HasValue)
        {
            var oldDocument = await context.DocumentReferences
                .FirstOrDefaultAsync(d => d.Id == product.ImageDocumentId.Value, cancellationToken);

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

        // Update product with new DocumentReference ID
        product.ImageDocumentId = documentReference.Id;
        product.ModifiedAt = DateTime.UtcNow;
        product.ModifiedBy = "System";

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Product {ProductId} image uploaded successfully as DocumentReference {DocumentId}.", productId, documentReference.Id);

        // Reload to get the document reference
        product.ImageDocument = documentReference;
        return MapToProductDto(product);
    }

    public async Task<Prym.DTOs.Teams.DocumentReferenceDto?> GetProductImageDocumentAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.ImageDocument)
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

        if (product?.ImageDocument is null)
        {
            logger.LogWarning("Product {ProductId} not found or has no image in tenant {TenantId}.", productId, currentTenantId.Value);
            return null;
        }

        return MapToDocumentReferenceDto(product.ImageDocument);
    }

    public async Task<bool> DeleteProductImageAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var currentTenantId = tenantContext.CurrentTenantId;
        if (!currentTenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required for product operations.");
        }

        var product = await context.Products
            .Include(p => p.ImageDocument)
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted && p.TenantId == currentTenantId.Value, cancellationToken);

        if (product?.ImageDocument is null)
        {
            logger.LogWarning("Product {ProductId} not found or has no image to delete in tenant {TenantId}.", productId, currentTenantId.Value);
            return false;
        }

        // Delete physical file
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageDocument.StorageKey.TrimStart('/'));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Remove DocumentReference
        _ = context.DocumentReferences.Remove(product.ImageDocument);

        // Update product
        product.ImageDocumentId = null;
        product.ModifiedAt = DateTime.UtcNow;
        product.ModifiedBy = "System";

        _ = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Product {ProductId} image deleted successfully.", productId);
        return true;
    }

    // Private mapping methods

}
