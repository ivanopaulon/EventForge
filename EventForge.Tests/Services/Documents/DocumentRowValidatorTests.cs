using EventForge.Client.Services.Documents;
using EventForge.DTOs.Documents;
using EventForge.DTOs.Common;
using Xunit;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentRowValidator
/// </summary>
[Trait("Category", "Unit")]
public class DocumentRowValidatorTests
{
    private readonly DocumentRowValidator _validator = new();

    [Fact]
    public void Validate_WithValidDto_ReturnsSuccess()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test Product",
            Quantity = 10,
            UnitPrice = 100.50m,
            VatRate = 22,
            UnitOfMeasureId = Guid.NewGuid(),
            LineDiscount = 5,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithEmptyDescription_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "",
            Quantity = 10,
            UnitPrice = 100,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.descriptionRequired", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithWhitespaceDescription_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "   ",
            Quantity = 10,
            UnitPrice = 100,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.descriptionRequired", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithZeroQuantity_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 0,
            UnitPrice = 100,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.quantityMustBePositive", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithNegativeQuantity_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = -5,
            UnitPrice = 100,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.quantityMustBePositive", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithTooLargeQuantity_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 1000000, // > 999999
            UnitPrice = 100,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.quantityTooLarge", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithNegativeUnitPrice_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = -100,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.unitPriceCannotBeNegative", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithTooLargeUnitPrice_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 10000000m, // > 9999999.99
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.unitPriceTooLarge", result.ErrorKeys);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(101)]
    [InlineData(150)]
    public void Validate_WithInvalidDiscountPercentage_ReturnsError(decimal discount)
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            LineDiscount = discount,
            DiscountType = DiscountType.Percentage,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.discountPercentageInvalid", result.ErrorKeys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_WithValidDiscountPercentage_ReturnsSuccess(decimal discount)
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            LineDiscount = discount,
            DiscountType = DiscountType.Percentage,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithNegativeDiscountValue_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            LineDiscountValue = -50,
            DiscountType = DiscountType.Value,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.discountValueCannotBeNegative", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithDiscountValueExceedingTotal_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            LineDiscountValue = 1500, // Total is 1000
            DiscountType = DiscountType.Value,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.discountValueExceedsTotal", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithDiscountValueEqualToTotal_ReturnsSuccess()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            LineDiscountValue = 1000, // Total is 1000
            DiscountType = DiscountType.Value,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_WithInvalidVatRate_ReturnsError(decimal vatRate)
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            VatRate = vatRate,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.vatRateInvalid", result.ErrorKeys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(22)]
    [InlineData(100)]
    public void Validate_WithValidVatRate_ReturnsSuccess(decimal vatRate)
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            VatRate = vatRate,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithoutUnitOfMeasure_ReturnsError()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            UnitOfMeasureId = null,
            UnitOfMeasure = null
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.unitOfMeasureRequired", result.ErrorKeys);
    }

    [Fact]
    public void Validate_WithUnitOfMeasureId_ReturnsSuccess()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            UnitOfMeasureId = Guid.NewGuid(),
            UnitOfMeasure = null
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithUnitOfMeasureString_ReturnsSuccess()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "Test",
            Quantity = 10,
            UnitPrice = 100,
            UnitOfMeasureId = null,
            UnitOfMeasure = "PCS"
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var dto = new CreateDocumentRowDto
        {
            Description = "",
            Quantity = -5,
            UnitPrice = -100,
            VatRate = 150,
            UnitOfMeasureId = null,
            UnitOfMeasure = null
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(5, result.ErrorKeys.Count);
        Assert.Contains("validation.descriptionRequired", result.ErrorKeys);
        Assert.Contains("validation.quantityMustBePositive", result.ErrorKeys);
        Assert.Contains("validation.unitPriceCannotBeNegative", result.ErrorKeys);
        Assert.Contains("validation.vatRateInvalid", result.ErrorKeys);
        Assert.Contains("validation.unitOfMeasureRequired", result.ErrorKeys);
    }

    [Fact]
    public void Validate_UpdateDto_UsesCreateDtoValidation()
    {
        // Arrange
        var updateDto = new UpdateDocumentRowDto
        {
            Description = "",
            Quantity = -5,
            UnitPrice = 100
        };

        // Act
        var result = _validator.Validate(updateDto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("validation.descriptionRequired", result.ErrorKeys);
        Assert.Contains("validation.quantityMustBePositive", result.ErrorKeys);
    }

    [Fact]
    public void Validate_UpdateDtoWithValidData_ReturnsSuccess()
    {
        // Arrange
        var updateDto = new UpdateDocumentRowDto
        {
            Description = "Test Product",
            Quantity = 10,
            UnitPrice = 100.50m,
            VatRate = 22,
            UnitOfMeasureId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(updateDto);

        // Assert
        Assert.True(result.IsValid);
    }
}
