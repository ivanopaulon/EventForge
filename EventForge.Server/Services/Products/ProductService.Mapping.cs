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
    private static ProductDto MapToPosCatalogDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Code = product.Code,
            ThumbnailUrl = product.ImageDocument?.Url ?? product.ImageDocument?.ThumbnailStorageKey ?? product.ImageDocument?.StorageKey,
            ImageDocumentId = product.ImageDocumentId,
            DefaultPrice = product.DefaultPrice,
            VatRateId = product.VatRateId,
            VatRateName = product.VatRate?.Name,
            CategoryNodeId = product.CategoryNodeId,
            Status = (Prym.DTOs.Common.ProductStatus)product.Status,
            IsVatIncluded = product.IsVatIncluded
        };
    }

    private static ProductDto MapToProductDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Code = product.Code,
            ImageDocumentId = product.ImageDocumentId,
            ThumbnailUrl = product.ImageDocument?.Url ?? product.ImageDocument?.ThumbnailStorageKey ?? product.ImageDocument?.StorageKey,
            Status = (Prym.DTOs.Common.ProductStatus)product.Status,
            IsActive = product.IsActive,
            IsVatIncluded = product.IsVatIncluded,
            DefaultPrice = product.DefaultPrice,
            VatRateId = product.VatRateId,
            VatRateName = product.VatRate?.Name,
            VatRatePercentage = product.VatRate?.Percentage,
            UnitOfMeasureId = product.UnitOfMeasureId,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            ModelId = product.ModelId,
            ModelName = product.Model?.Name,
            PreferredSupplierId = product.PreferredSupplierId,
            ReorderPoint = product.ReorderPoint,
            SafetyStock = product.SafetyStock,
            TargetStockLevel = product.TargetStockLevel,
            AverageDailyDemand = product.AverageDailyDemand,
            CategoryNodeId = product.CategoryNodeId,
            FamilyNodeId = product.FamilyNodeId,
            GroupNodeId = product.GroupNodeId,
            StationId = product.StationId,
            IsBundle = product.IsBundle,
            CodeCount = product.Codes.Count(c => !c.IsDeleted),
            UnitCount = product.Units.Count(u => !u.IsDeleted),
            BundleItemCount = product.BundleItems.Count(bi => !bi.IsDeleted),
            CreatedAt = product.CreatedAt,
            CreatedBy = product.CreatedBy,
            ModifiedAt = product.ModifiedAt,
            ModifiedBy = product.ModifiedBy
        };
    }

    private static ProductDetailDto MapToProductDetailDto(Product product)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            Description = product.Description,
            Code = product.Code,
            ImageDocumentId = product.ImageDocumentId,
            ThumbnailUrl = product.ImageDocument?.Url ?? product.ImageDocument?.ThumbnailStorageKey ?? product.ImageDocument?.StorageKey,
            Status = (Prym.DTOs.Common.ProductStatus)product.Status,
            IsVatIncluded = product.IsVatIncluded,
            DefaultPrice = product.DefaultPrice,
            VatRateId = product.VatRateId,
            UnitOfMeasureId = product.UnitOfMeasureId,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            ModelId = product.ModelId,
            ModelName = product.Model?.Name,
            PreferredSupplierId = product.PreferredSupplierId,
            PreferredSupplierName = null, // resolved on client via suppliers list
            ReorderPoint = product.ReorderPoint,
            SafetyStock = product.SafetyStock,
            TargetStockLevel = product.TargetStockLevel,
            AverageDailyDemand = product.AverageDailyDemand,
            CategoryNodeId = product.CategoryNodeId,
            FamilyNodeId = product.FamilyNodeId,
            GroupNodeId = product.GroupNodeId,
            StationId = product.StationId,
            IsBundle = product.IsBundle,
            Codes = product.Codes.Where(c => !c.IsDeleted).Select(MapToProductCodeDto),
            Units = product.Units.Where(u => !u.IsDeleted).Select(MapToProductUnitDto),
            BundleItems = product.BundleItems.Where(bi => !bi.IsDeleted).Select(MapToProductBundleItemDto),
            CreatedAt = product.CreatedAt,
            CreatedBy = product.CreatedBy,
            ModifiedAt = product.ModifiedAt,
            ModifiedBy = product.ModifiedBy
        };
    }

    private static ProductCodeDto MapToProductCodeDto(ProductCode productCode)
    {
        return new ProductCodeDto
        {
            Id = productCode.Id,
            ProductId = productCode.ProductId,
            ProductUnitId = productCode.ProductUnitId,
            CodeType = productCode.CodeType,
            Code = productCode.Code,
            AlternativeDescription = productCode.AlternativeDescription,
            Status = (Prym.DTOs.Common.ProductCodeStatus)productCode.Status,
            UnitOfMeasureId = productCode.ProductUnit?.UnitOfMeasureId,
            UnitOfMeasureName = productCode.ProductUnit?.UnitOfMeasure?.Name,
            ConversionFactor = productCode.ProductUnit?.ConversionFactor,
            CreatedAt = productCode.CreatedAt,
            CreatedBy = productCode.CreatedBy,
            ModifiedAt = productCode.ModifiedAt,
            ModifiedBy = productCode.ModifiedBy
        };
    }

    private static ProductUnitDto MapToProductUnitDto(ProductUnit productUnit)
    {
        return new ProductUnitDto
        {
            Id = productUnit.Id,
            ProductId = productUnit.ProductId,
            UnitOfMeasureId = productUnit.UnitOfMeasureId,
            ConversionFactor = productUnit.ConversionFactor,
            UnitType = productUnit.UnitType,
            Description = productUnit.Description,
            Status = (Prym.DTOs.Common.ProductUnitStatus)productUnit.Status,
            CreatedAt = productUnit.CreatedAt,
            CreatedBy = productUnit.CreatedBy,
            ModifiedAt = productUnit.ModifiedAt,
            ModifiedBy = productUnit.ModifiedBy
        };
    }

    private static ProductBundleItemDto MapToProductBundleItemDto(ProductBundleItem bundleItem)
    {
        return new ProductBundleItemDto
        {
            Id = bundleItem.Id,
            BundleProductId = bundleItem.BundleProductId,
            ComponentProductId = bundleItem.ComponentProductId,
            Quantity = bundleItem.Quantity,
            ComponentProductName = bundleItem.ComponentProduct?.Name,
            ComponentProductCode = bundleItem.ComponentProduct?.Code,
            CreatedAt = bundleItem.CreatedAt,
            CreatedBy = bundleItem.CreatedBy,
            ModifiedAt = bundleItem.ModifiedAt,
            ModifiedBy = bundleItem.ModifiedBy
        };
    }

    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);
    }

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

    // Product Supplier management operations

}
