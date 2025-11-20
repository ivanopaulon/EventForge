using EventForge.DTOs.Warehouse;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Tests.DTOs;

/// <summary>
/// Unit tests for AddInventoryDocumentRowDto to verify MergeDuplicateProducts functionality.
/// </summary>
[Trait("Category", "Unit")]
public class AddInventoryDocumentRowDtoTests
{
    [Fact]
    public void MergeDuplicateProducts_DefaultsToFalse()
    {
        // Arrange & Act
        var dto = new AddInventoryDocumentRowDto
        {
            ProductId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Quantity = 10
        };

        // Assert
        Assert.False(dto.MergeDuplicateProducts);
    }

    [Fact]
    public void MergeDuplicateProducts_CanBeSetToTrue()
    {
        // Arrange & Act
        var dto = new AddInventoryDocumentRowDto
        {
            ProductId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Quantity = 10,
            MergeDuplicateProducts = true
        };

        // Assert
        Assert.True(dto.MergeDuplicateProducts);
    }

    [Fact]
    public void AddInventoryDocumentRowDto_WithDefaultGuidValues_IsStillValid()
    {
        // Arrange
        // Note: Guid.Empty (default(Guid)) is technically valid from validation perspective
        // Business logic should handle empty Guids, not validation attributes
        var dto = new AddInventoryDocumentRowDto
        {
            ProductId = Guid.Empty,
            LocationId = Guid.Empty,
            Quantity = 0,
            MergeDuplicateProducts = true
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert - Empty Guids pass validation (business logic handles them)
        Assert.True(isValid);
    }

    [Fact]
    public void AddInventoryDocumentRowDto_WithAllRequiredFields_IsValid()
    {
        // Arrange
        var dto = new AddInventoryDocumentRowDto
        {
            ProductId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Quantity = 15.5m,
            UnitOfMeasureId = Guid.NewGuid(),
            Notes = "Test inventory count",
            MergeDuplicateProducts = true
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void AddInventoryDocumentRowDto_WithNegativeQuantity_IsInvalid()
    {
        // Arrange
        var dto = new AddInventoryDocumentRowDto
        {
            ProductId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Quantity = -5,
            MergeDuplicateProducts = true
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddInventoryDocumentRowDto.Quantity)));
    }

    [Fact]
    public void AddInventoryDocumentRowDto_NotesTooLong_IsInvalid()
    {
        // Arrange
        var dto = new AddInventoryDocumentRowDto
        {
            ProductId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            Quantity = 10,
            Notes = new string('x', 201), // 201 characters, limit is 200
            MergeDuplicateProducts = true
        };

        // Act
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddInventoryDocumentRowDto.Notes)));
    }
}
