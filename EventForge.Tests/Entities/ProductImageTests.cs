using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Data.Entities.Teams;
using EventForge.DTOs.Common;
using Xunit;

namespace EventForge.Tests.Entities;

/// <summary>
/// Unit tests for Product image management with DocumentReference.
/// These tests verify the implementation of Issue #314.
/// </summary>
public class ProductImageTests
{
    [Fact]
    public void Product_ShouldInitialize_WithNullImageDocumentId()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        Assert.Null(product.ImageDocumentId);
        Assert.Null(product.ImageDocument);
    }

    [Fact]
    public void Product_ShouldAccept_ValidImageDocumentId()
    {
        // Arrange
        var imageDocumentId = Guid.NewGuid();

        // Act
        var product = new Product
        {
            Name = "Test Product",
            Code = "TEST-001",
            ImageDocumentId = imageDocumentId
        };

        // Assert
        Assert.Equal(imageDocumentId, product.ImageDocumentId);
    }

    [Fact]
    public void Product_ShouldAccept_ImageDocumentNavigationProperty()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        
        var documentReference = new DocumentReference
        {
            Id = documentId,
            OwnerId = productId,
            OwnerType = "Product",
            FileName = "product_image.jpg",
            Type = DocumentReferenceType.ProfilePhoto,
            SubType = DocumentReferenceSubType.None,
            MimeType = "image/jpeg",
            StorageKey = "/images/products/product_image.jpg",
            FileSizeBytes = 1024 * 100 // 100 KB
        };

        // Act
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Code = "TEST-001",
            ImageDocumentId = documentId,
            ImageDocument = documentReference
        };

        // Assert
        Assert.Equal(documentId, product.ImageDocumentId);
        Assert.NotNull(product.ImageDocument);
        Assert.Equal(documentId, product.ImageDocument.Id);
        Assert.Equal(productId, product.ImageDocument.OwnerId);
        Assert.Equal("Product", product.ImageDocument.OwnerType);
        Assert.Equal("image/jpeg", product.ImageDocument.MimeType);
    }

    [Fact]
    public void Product_ImageUrl_ShouldBeMarkedObsolete()
    {
        // Arrange
        var product = new Product();

        // Act - Check that ImageUrl property exists but is marked obsolete
        var imageUrlProperty = typeof(Product).GetProperty(nameof(Product.ImageUrl));

        // Assert
        Assert.NotNull(imageUrlProperty);
        var obsoleteAttribute = imageUrlProperty?.GetCustomAttributes(typeof(ObsoleteAttribute), false).FirstOrDefault() as ObsoleteAttribute;
        Assert.NotNull(obsoleteAttribute);
        Assert.Contains("ImageDocument", obsoleteAttribute.Message);
    }

    [Fact]
    public void Product_ShouldSupport_BothImageUrlAndImageDocumentId()
    {
        // Arrange
        var imageDocumentId = Guid.NewGuid();
        var imageUrl = "/images/products/legacy_image.jpg";

        // Act
        var product = new Product
        {
            Name = "Test Product",
            Code = "TEST-001",
#pragma warning disable CS0618 // Type or member is obsolete
            ImageUrl = imageUrl,
#pragma warning restore CS0618 // Type or member is obsolete
            ImageDocumentId = imageDocumentId
        };

        // Assert
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal(imageUrl, product.ImageUrl);
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.Equal(imageDocumentId, product.ImageDocumentId);
    }

    [Fact]
    public void DocumentReference_ForProductImage_ShouldHaveValidProperties()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var documentReference = new DocumentReference
        {
            TenantId = tenantId,
            OwnerId = productId,
            OwnerType = "Product",
            FileName = "product_12345.jpg",
            Type = DocumentReferenceType.ProfilePhoto,
            SubType = DocumentReferenceSubType.None,
            MimeType = "image/jpeg",
            StorageKey = "/images/products/product_12345.jpg",
            Url = "/images/products/product_12345.jpg",
            ThumbnailStorageKey = "/images/products/product_12345_thumb.jpg",
            FileSizeBytes = 2048 * 1024, // 2 MB
            Title = "Product Image",
            Notes = "Product catalog image"
        };

        // Assert
        Assert.Equal(tenantId, documentReference.TenantId);
        Assert.Equal(productId, documentReference.OwnerId);
        Assert.Equal("Product", documentReference.OwnerType);
        Assert.Equal("product_12345.jpg", documentReference.FileName);
        Assert.Equal(DocumentReferenceType.ProfilePhoto, documentReference.Type);
        Assert.Equal("image/jpeg", documentReference.MimeType);
        Assert.Equal("/images/products/product_12345.jpg", documentReference.StorageKey);
        Assert.Equal("/images/products/product_12345_thumb.jpg", documentReference.ThumbnailStorageKey);
        Assert.Equal(2048 * 1024, documentReference.FileSizeBytes);
        Assert.Equal("Product Image", documentReference.Title);
    }

    [Fact]
    public void Product_ImageDocumentId_CanBeNull()
    {
        // Arrange & Act
        var product = new Product
        {
            Name = "Test Product",
            Code = "TEST-001",
            ImageDocumentId = null
        };

        // Assert
        Assert.Null(product.ImageDocumentId);
        Assert.False(product.ImageDocumentId.HasValue);
    }

    [Fact]
    public void Product_ImageDocumentId_CanBeUpdated()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Code = "TEST-001",
            ImageDocumentId = null
        };

        var newImageDocumentId = Guid.NewGuid();

        // Act
        product.ImageDocumentId = newImageDocumentId;

        // Assert
        Assert.Equal(newImageDocumentId, product.ImageDocumentId);
        Assert.True(product.ImageDocumentId.HasValue);
    }

    [Fact]
    public void Product_ImageDocumentId_CanBeCleared()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Code = "TEST-001",
            ImageDocumentId = Guid.NewGuid()
        };

        // Act
        product.ImageDocumentId = null;

        // Assert
        Assert.Null(product.ImageDocumentId);
        Assert.False(product.ImageDocumentId.HasValue);
    }
}
