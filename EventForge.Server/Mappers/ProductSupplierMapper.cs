using EventForge.DTOs.Products;
using EventForge.Server.Data.Entities.Products;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for ProductSupplier entity to DTOs.
/// </summary>
public static class ProductSupplierMapper
{
    /// <summary>
    /// Maps ProductSupplier entity to ProductSupplierDto.
    /// </summary>
    public static ProductSupplierDto ToDto(ProductSupplier productSupplier)
    {
        return new ProductSupplierDto
        {
            Id = productSupplier.Id,
            ProductId = productSupplier.ProductId,
            ProductName = productSupplier.Product?.Name,
            SupplierId = productSupplier.SupplierId,
            SupplierName = productSupplier.Supplier?.Name,
            SupplierProductCode = productSupplier.SupplierProductCode,
            PurchaseDescription = productSupplier.PurchaseDescription,
            UnitCost = productSupplier.UnitCost,
            Currency = productSupplier.Currency,
            MinOrderQty = productSupplier.MinOrderQty,
            IncrementQty = productSupplier.IncrementQty,
            LeadTimeDays = productSupplier.LeadTimeDays,
            LastPurchasePrice = productSupplier.LastPurchasePrice,
            LastPurchaseDate = productSupplier.LastPurchaseDate,
            Preferred = productSupplier.Preferred,
            Notes = productSupplier.Notes,
            CreatedAt = productSupplier.CreatedAt,
            CreatedBy = productSupplier.CreatedBy
        };
    }

    /// <summary>
    /// Maps collection of ProductSupplier entities to ProductSupplierDto collection.
    /// </summary>
    public static IEnumerable<ProductSupplierDto> ToDtoCollection(IEnumerable<ProductSupplier> productSuppliers)
    {
        return productSuppliers.Select(ToDto);
    }

    /// <summary>
    /// Maps collection of ProductSupplier entities to ProductSupplierDto list.
    /// </summary>
    public static List<ProductSupplierDto> ToDtoList(IEnumerable<ProductSupplier> productSuppliers)
    {
        return productSuppliers.Select(ToDto).ToList();
    }

    /// <summary>
    /// Maps CreateProductSupplierDto to ProductSupplier entity.
    /// </summary>
    public static ProductSupplier ToEntity(CreateProductSupplierDto dto)
    {
        return new ProductSupplier
        {
            ProductId = dto.ProductId,
            SupplierId = dto.SupplierId,
            SupplierProductCode = dto.SupplierProductCode,
            PurchaseDescription = dto.PurchaseDescription,
            UnitCost = dto.UnitCost,
            Currency = dto.Currency,
            MinOrderQty = dto.MinOrderQty,
            IncrementQty = dto.IncrementQty,
            LeadTimeDays = dto.LeadTimeDays,
            LastPurchasePrice = dto.LastPurchasePrice,
            LastPurchaseDate = dto.LastPurchaseDate,
            Preferred = dto.Preferred,
            Notes = dto.Notes
        };
    }

    /// <summary>
    /// Updates ProductSupplier entity from UpdateProductSupplierDto.
    /// </summary>
    public static void UpdateEntity(ProductSupplier entity, UpdateProductSupplierDto dto)
    {
        entity.SupplierProductCode = dto.SupplierProductCode;
        entity.PurchaseDescription = dto.PurchaseDescription;
        entity.UnitCost = dto.UnitCost;
        entity.Currency = dto.Currency;
        entity.MinOrderQty = dto.MinOrderQty;
        entity.IncrementQty = dto.IncrementQty;
        entity.LeadTimeDays = dto.LeadTimeDays;
        entity.LastPurchasePrice = dto.LastPurchasePrice;
        entity.LastPurchaseDate = dto.LastPurchaseDate;
        entity.Preferred = dto.Preferred;
        entity.Notes = dto.Notes;
    }
}
