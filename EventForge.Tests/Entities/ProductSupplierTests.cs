using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Business;
using Xunit;

namespace EventForge.Tests.Entities;

/// <summary>
/// Unit tests for ProductSupplier entity business rules and constraints.
/// These tests verify the data model constraints defined in Issue #353.
/// </summary>
public class ProductSupplierTests
{
    [Fact]
    public void ProductSupplier_ShouldInitialize_WithDefaultValues()
    {
        // Arrange & Act
        var productSupplier = new ProductSupplier();

        // Assert
        Assert.NotEqual(Guid.Empty, productSupplier.Id);
        Assert.False(productSupplier.Preferred);
        Assert.False(productSupplier.IsDeleted);
        Assert.True(productSupplier.IsActive);
    }

    [Fact]
    public void ProductSupplier_ShouldAccept_ValidSupplierData()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        // Act
        var productSupplier = new ProductSupplier
        {
            ProductId = productId,
            SupplierId = supplierId,
            SupplierProductCode = "SUPP-001",
            UnitCost = 99.99m,
            Currency = "EUR",
            MinOrderQty = 10,
            IncrementQty = 5,
            LeadTimeDays = 7,
            Preferred = true
        };

        // Assert
        Assert.Equal(productId, productSupplier.ProductId);
        Assert.Equal(supplierId, productSupplier.SupplierId);
        Assert.Equal("SUPP-001", productSupplier.SupplierProductCode);
        Assert.Equal(99.99m, productSupplier.UnitCost);
        Assert.Equal("EUR", productSupplier.Currency);
        Assert.Equal(10, productSupplier.MinOrderQty);
        Assert.Equal(5, productSupplier.IncrementQty);
        Assert.Equal(7, productSupplier.LeadTimeDays);
        Assert.True(productSupplier.Preferred);
    }

    [Fact]
    public void Brand_ShouldInitialize_WithDefaultValues()
    {
        // Arrange & Act
        var brand = new Brand();

        // Assert
        Assert.NotEqual(Guid.Empty, brand.Id);
        Assert.False(brand.IsDeleted);
        Assert.True(brand.IsActive);
        Assert.NotNull(brand.Models);
        Assert.NotNull(brand.Products);
    }

    [Fact]
    public void Brand_ShouldAccept_ValidBrandData()
    {
        // Arrange & Act
        var brand = new Brand
        {
            Name = "Samsung",
            Description = "Electronics manufacturer",
            Website = "https://www.samsung.com",
            Country = "South Korea"
        };

        // Assert
        Assert.Equal("Samsung", brand.Name);
        Assert.Equal("Electronics manufacturer", brand.Description);
        Assert.Equal("https://www.samsung.com", brand.Website);
        Assert.Equal("South Korea", brand.Country);
    }

    [Fact]
    public void Model_ShouldInitialize_WithDefaultValues()
    {
        // Arrange & Act
        var model = new Model();

        // Assert
        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.False(model.IsDeleted);
        Assert.True(model.IsActive);
        Assert.NotNull(model.Products);
    }

    [Fact]
    public void Model_ShouldAccept_ValidModelData()
    {
        // Arrange
        var brandId = Guid.NewGuid();

        // Act
        var model = new Model
        {
            BrandId = brandId,
            Name = "Galaxy S23",
            Description = "Flagship smartphone",
            ManufacturerPartNumber = "SM-S911"
        };

        // Assert
        Assert.Equal(brandId, model.BrandId);
        Assert.Equal("Galaxy S23", model.Name);
        Assert.Equal("Flagship smartphone", model.Description);
        Assert.Equal("SM-S911", model.ManufacturerPartNumber);
    }

    [Fact]
    public void Product_ShouldAccept_NewBrandModelFields()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var modelId = Guid.NewGuid();
        var preferredSupplierId = Guid.NewGuid();

        // Act
        var product = new Product
        {
            Name = "Test Product",
            Code = "TEST-001",
            BrandId = brandId,
            ModelId = modelId,
            PreferredSupplierId = preferredSupplierId,
            ReorderPoint = 20m,
            SafetyStock = 10m,
            TargetStockLevel = 50m,
            AverageDailyDemand = 5m
        };

        // Assert
        Assert.Equal(brandId, product.BrandId);
        Assert.Equal(modelId, product.ModelId);
        Assert.Equal(preferredSupplierId, product.PreferredSupplierId);
        Assert.Equal(20m, product.ReorderPoint);
        Assert.Equal(10m, product.SafetyStock);
        Assert.Equal(50m, product.TargetStockLevel);
        Assert.Equal(5m, product.AverageDailyDemand);
    }

    [Fact]
    public void Product_ShouldInitialize_SuppliersCollection()
    {
        // Arrange & Act
        var product = new Product
        {
            Name = "Test Product",
            Code = "TEST-001"
        };

        // Assert
        Assert.NotNull(product.Suppliers);
        Assert.Empty(product.Suppliers);
    }
}

/// <summary>
/// Unit tests documenting business rules that should be enforced at the service layer.
/// These are documentation tests - actual validation should be implemented in services.
/// </summary>
public class ProductSupplierBusinessRulesTests
{
    [Fact]
    public void DocumentedRule_OnlyOnePreferredSupplierPerProduct()
    {
        // This test documents the business rule:
        // Only one ProductSupplier per Product can have Preferred = true.
        // 
        // Implementation should be in the service layer:
        // 1. When setting Preferred = true, check if another supplier is already preferred
        // 2. If yes, either reject or automatically set the existing one to false
        //
        // Example validation:
        // var existingPreferred = await productSupplierRepo.GetPreferredAsync(productId);
        // if (existingPreferred != null && existingPreferred.Id != currentSupplierId)
        // {
        //     existingPreferred.Preferred = false;
        //     await productSupplierRepo.UpdateAsync(existingPreferred);
        // }

        Assert.True(true, "Business rule documented: Only one preferred supplier per product");
    }

    [Fact]
    public void DocumentedRule_BundlesCannotHaveSuppliers()
    {
        // This test documents the business rule:
        // If Product.IsBundle = true, the product cannot have ProductSupplier records.
        //
        // Implementation should be in the service layer:
        // 1. When creating/updating ProductSupplier, verify Product.IsBundle = false
        // 2. When setting Product.IsBundle = true, verify no ProductSuppliers exist
        //
        // Example validation:
        // if (product.IsBundle)
        // {
        //     throw new ValidationException("Bundle products cannot have suppliers");
        // }

        Assert.True(true, "Business rule documented: Bundles cannot have suppliers");
    }

    [Fact]
    public void DocumentedRule_SupplierMustBeCorrectPartyType()
    {
        // This test documents the business rule:
        // ProductSupplier.SupplierId must reference a BusinessParty with
        // PartyType = Fornitore or ClienteFornitore.
        //
        // Implementation should be in the service layer:
        // 1. When creating/updating ProductSupplier, verify the BusinessParty PartyType
        //
        // Example validation:
        // var supplier = await businessPartyRepo.GetByIdAsync(supplierId);
        // if (supplier.PartyType != BusinessPartyType.Fornitore && 
        //     supplier.PartyType != BusinessPartyType.ClienteFornitore)
        // {
        //     throw new ValidationException("Supplier must be of type Fornitore or ClienteFornitore");
        // }

        Assert.True(true, "Business rule documented: Supplier must have correct PartyType");
    }
}
