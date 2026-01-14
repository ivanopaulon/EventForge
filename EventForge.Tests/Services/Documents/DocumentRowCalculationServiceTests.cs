using EventForge.Client.Models.Documents;
using EventForge.Client.Services.Documents;
using EventForge.DTOs.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentRowCalculationService to verify fiscal calculations.
/// Tests cover VAT calculations, discounts, and edge cases.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentRowCalculationServiceTests
{
    private readonly IDocumentRowCalculationService _service;
    private readonly Mock<ILogger<DocumentRowCalculationService>> _loggerMock;

    public DocumentRowCalculationServiceTests()
    {
        _loggerMock = new Mock<ILogger<DocumentRowCalculationService>>();
        _service = new DocumentRowCalculationService(_loggerMock.Object);
    }

    #region CalculateRowTotals Tests

    [Fact]
    public void CalculateRowTotals_WithVat22_ReturnsCorrectTotal()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = 2,
            UnitPrice = 10.00m,
            VatRate = 22,
            DiscountPercentage = 0,
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var result = _service.CalculateRowTotals(input);

        // Assert
        Assert.Equal(20.00m, result.GrossAmount); // 2 × 10
        Assert.Equal(0.00m, result.DiscountAmount); // No discount
        Assert.Equal(20.00m, result.NetAmount); // 20 - 0
        Assert.Equal(4.40m, result.VatAmount); // 20 × 0.22
        Assert.Equal(24.40m, result.TotalAmount); // 20 + 4.40
        Assert.Equal(12.20m, result.UnitPriceGross); // 10 × 1.22
    }

    [Fact]
    public void CalculateRowTotals_WithVat0_ReturnsNoVat()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = 1,
            UnitPrice = 100.00m,
            VatRate = 0,
            DiscountPercentage = 0,
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var result = _service.CalculateRowTotals(input);

        // Assert
        Assert.Equal(100.00m, result.NetAmount);
        Assert.Equal(0.00m, result.VatAmount);
        Assert.Equal(100.00m, result.TotalAmount);
    }

    [Fact]
    public void CalculateRowTotals_WithPercentageDiscount_AppliesCorrectly()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = 2,
            UnitPrice = 100.00m,
            VatRate = 22,
            DiscountPercentage = 10, // 10% discount
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var result = _service.CalculateRowTotals(input);

        // Assert
        Assert.Equal(200.00m, result.GrossAmount); // 2 × 100
        Assert.Equal(20.00m, result.DiscountAmount); // 200 × 0.10
        Assert.Equal(180.00m, result.NetAmount); // 200 - 20
        Assert.Equal(39.60m, result.VatAmount); // 180 × 0.22
        Assert.Equal(219.60m, result.TotalAmount); // 180 + 39.60
    }

    [Fact]
    public void CalculateRowTotals_WithFixedDiscount_AppliesCorrectly()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = 2,
            UnitPrice = 100.00m,
            VatRate = 22,
            DiscountPercentage = 0,
            DiscountValue = 30.00m, // 30€ fixed discount
            DiscountType = DiscountType.Value
        };

        // Act
        var result = _service.CalculateRowTotals(input);

        // Assert
        Assert.Equal(200.00m, result.GrossAmount); // 2 × 100
        Assert.Equal(30.00m, result.DiscountAmount); // Fixed 30
        Assert.Equal(170.00m, result.NetAmount); // 200 - 30
        Assert.Equal(37.40m, result.VatAmount); // 170 × 0.22
        Assert.Equal(207.40m, result.TotalAmount); // 170 + 37.40
    }

    [Fact]
    public void CalculateRowTotals_WithDecimalQuantity_CalculatesCorrectly()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = 0.5m,
            UnitPrice = 10.00m,
            VatRate = 22,
            DiscountPercentage = 0,
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var result = _service.CalculateRowTotals(input);

        // Assert
        Assert.Equal(5.00m, result.GrossAmount); // 0.5 × 10
        Assert.Equal(5.00m, result.NetAmount);
        Assert.Equal(1.10m, result.VatAmount); // 5 × 0.22
        Assert.Equal(6.10m, result.TotalAmount); // 5 + 1.10
    }

    [Fact]
    public void CalculateRowTotals_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.CalculateRowTotals(null!));
    }

    [Fact]
    public void CalculateRowTotals_WithNegativeQuantity_ThrowsArgumentException()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = -1,
            UnitPrice = 10.00m,
            VatRate = 22,
            DiscountPercentage = 0,
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CalculateRowTotals(input));
    }

    [Fact]
    public void CalculateRowTotals_WithNegativeUnitPrice_ThrowsArgumentException()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = 1,
            UnitPrice = -10.00m,
            VatRate = 22,
            DiscountPercentage = 0,
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CalculateRowTotals(input));
    }

    [Fact]
    public void CalculateRowTotals_WithVatRateOver100_ThrowsArgumentException()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = 1,
            UnitPrice = 10.00m,
            VatRate = 150, // Invalid
            DiscountPercentage = 0,
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CalculateRowTotals(input));
    }

    #endregion

    #region ExtractVat Tests

    [Fact]
    public void ExtractVat_WithVat22_ReturnsNetPrice()
    {
        // Arrange
        decimal grossPrice = 122.00m;
        decimal vatRate = 22m;

        // Act
        decimal netPrice = _service.ExtractVat(grossPrice, vatRate);

        // Assert
        Assert.Equal(100.00m, netPrice); // 122 / 1.22 = 100
    }

    [Fact]
    public void ExtractVat_WithVat0_ReturnsSamePrice()
    {
        // Arrange
        decimal grossPrice = 100.00m;
        decimal vatRate = 0m;

        // Act
        decimal netPrice = _service.ExtractVat(grossPrice, vatRate);

        // Assert
        Assert.Equal(100.00m, netPrice);
    }

    [Fact]
    public void ExtractVat_WithVat10_ReturnsCorrectNetPrice()
    {
        // Arrange
        decimal grossPrice = 110.00m;
        decimal vatRate = 10m;

        // Act
        decimal netPrice = _service.ExtractVat(grossPrice, vatRate);

        // Assert
        Assert.Equal(100.00m, netPrice); // 110 / 1.10 = 100
    }

    #endregion

    #region ApplyVat Tests

    [Fact]
    public void ApplyVat_WithVat22_ReturnsGrossPrice()
    {
        // Arrange
        decimal netPrice = 100.00m;
        decimal vatRate = 22m;

        // Act
        decimal grossPrice = _service.ApplyVat(netPrice, vatRate);

        // Assert
        Assert.Equal(122.00m, grossPrice); // 100 × 1.22 = 122
    }

    [Fact]
    public void ApplyVat_WithVat0_ReturnsSamePrice()
    {
        // Arrange
        decimal netPrice = 100.00m;
        decimal vatRate = 0m;

        // Act
        decimal grossPrice = _service.ApplyVat(netPrice, vatRate);

        // Assert
        Assert.Equal(100.00m, grossPrice);
    }

    #endregion

    #region CalculateDiscountAmount Tests

    [Fact]
    public void CalculateDiscountAmount_WithPercentage_ReturnsCorrectAmount()
    {
        // Arrange
        decimal baseAmount = 200.00m;
        decimal discountPercentage = 10m;
        decimal discountValue = 0m;
        DiscountType discountType = DiscountType.Percentage;

        // Act
        decimal discount = _service.CalculateDiscountAmount(baseAmount, discountPercentage, discountValue, discountType);

        // Assert
        Assert.Equal(20.00m, discount); // 200 × 0.10 = 20
    }

    [Fact]
    public void CalculateDiscountAmount_WithFixedValue_ReturnsCorrectAmount()
    {
        // Arrange
        decimal baseAmount = 200.00m;
        decimal discountPercentage = 0m;
        decimal discountValue = 30.00m;
        DiscountType discountType = DiscountType.Value;

        // Act
        decimal discount = _service.CalculateDiscountAmount(baseAmount, discountPercentage, discountValue, discountType);

        // Assert
        Assert.Equal(30.00m, discount);
    }

    [Fact]
    public void CalculateDiscountAmount_WithValueExceedingBase_ClampedToBase()
    {
        // Arrange
        decimal baseAmount = 100.00m;
        decimal discountPercentage = 0m;
        decimal discountValue = 200.00m; // Exceeds base
        DiscountType discountType = DiscountType.Value;

        // Act
        decimal discount = _service.CalculateDiscountAmount(baseAmount, discountPercentage, discountValue, discountType);

        // Assert
        Assert.Equal(100.00m, discount); // Clamped to base amount
    }

    [Fact]
    public void CalculateDiscountAmount_With100PercentDiscount_ReturnsBaseAmount()
    {
        // Arrange
        decimal baseAmount = 200.00m;
        decimal discountPercentage = 100m;
        decimal discountValue = 0m;
        DiscountType discountType = DiscountType.Percentage;

        // Act
        decimal discount = _service.CalculateDiscountAmount(baseAmount, discountPercentage, discountValue, discountType);

        // Assert
        Assert.Equal(200.00m, discount); // 200 × 1.00 = 200
    }

    #endregion

    #region CalculateVatAmount Tests

    [Fact]
    public void CalculateVatAmount_WithVat22_ReturnsCorrectVat()
    {
        // Arrange
        decimal netAmount = 100.00m;
        decimal vatRate = 22m;

        // Act
        decimal vatAmount = _service.CalculateVatAmount(netAmount, vatRate);

        // Assert
        Assert.Equal(22.00m, vatAmount); // 100 × 0.22 = 22
    }

    [Fact]
    public void CalculateVatAmount_WithVat0_ReturnsZero()
    {
        // Arrange
        decimal netAmount = 100.00m;
        decimal vatRate = 0m;

        // Act
        decimal vatAmount = _service.CalculateVatAmount(netAmount, vatRate);

        // Assert
        Assert.Equal(0.00m, vatAmount);
    }

    #endregion

    #region ConvertPrice Tests

    [Fact]
    public void ConvertPrice_FromGrossToNet_ExtractsVat()
    {
        // Arrange
        var input = new VatConversionInput
        {
            Price = 122.00m,
            VatRate = 22m,
            IsVatIncluded = true
        };

        // Act
        decimal netPrice = _service.ConvertPrice(input);

        // Assert
        Assert.Equal(100.00m, netPrice);
    }

    [Fact]
    public void ConvertPrice_FromNetToGross_AppliesVat()
    {
        // Arrange
        var input = new VatConversionInput
        {
            Price = 100.00m,
            VatRate = 22m,
            IsVatIncluded = false
        };

        // Act
        decimal grossPrice = _service.ConvertPrice(input);

        // Assert
        Assert.Equal(122.00m, grossPrice);
    }

    [Fact]
    public void ConvertPrice_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.ConvertPrice(null!));
    }

    #endregion

    #region Rounding Tests

    [Fact]
    public void CalculateRowTotals_RoundsTo2Decimals()
    {
        // Arrange
        var input = new DocumentRowCalculationInput
        {
            Quantity = 3,
            UnitPrice = 10.333333m,
            VatRate = 22,
            DiscountPercentage = 0,
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var result = _service.CalculateRowTotals(input);

        // Assert
        // All amounts should be rounded to 2 decimals
        Assert.Equal(31.00m, result.GrossAmount); // 3 × 10.333333 = 31.00 (rounded)
        Assert.Equal(31.00m, result.NetAmount);
        Assert.Equal(6.82m, result.VatAmount); // 31.00 × 0.22 = 6.82
        Assert.Equal(37.82m, result.TotalAmount); // 31.00 + 6.82
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void CalculateRowTotals_ComplexScenario_WithPercentageDiscountAndVat()
    {
        // Arrange - Real world scenario
        var input = new DocumentRowCalculationInput
        {
            Quantity = 5,
            UnitPrice = 25.50m,
            VatRate = 22,
            DiscountPercentage = 15, // 15% discount
            DiscountValue = 0,
            DiscountType = DiscountType.Percentage
        };

        // Act
        var result = _service.CalculateRowTotals(input);

        // Assert
        Assert.Equal(127.50m, result.GrossAmount); // 5 × 25.50 = 127.50
        Assert.Equal(19.13m, result.DiscountAmount); // 127.50 × 0.15 = 19.125 → 19.13 (rounded at end)
        Assert.Equal(108.38m, result.NetAmount); // 127.50 - 19.125 = 108.375 → 108.38 (rounded at end)
        Assert.Equal(23.84m, result.VatAmount); // 108.375 × 0.22 = 23.8425 → 23.84 (rounded at end)
        Assert.Equal(132.22m, result.TotalAmount); // 108.375 + 23.8425 = 132.2175 → 132.22 (rounded at end)
    }

    [Fact]
    public void CalculateRowTotals_ComplexScenario_WithFixedDiscountAndVat()
    {
        // Arrange - Real world scenario
        var input = new DocumentRowCalculationInput
        {
            Quantity = 3,
            UnitPrice = 50.00m,
            VatRate = 10,
            DiscountPercentage = 0,
            DiscountValue = 25.00m, // 25€ fixed discount
            DiscountType = DiscountType.Value
        };

        // Act
        var result = _service.CalculateRowTotals(input);

        // Assert
        Assert.Equal(150.00m, result.GrossAmount); // 3 × 50 = 150
        Assert.Equal(25.00m, result.DiscountAmount); // Fixed 25
        Assert.Equal(125.00m, result.NetAmount); // 150 - 25 = 125
        Assert.Equal(12.50m, result.VatAmount); // 125 × 0.10 = 12.50
        Assert.Equal(137.50m, result.TotalAmount); // 125 + 12.50 = 137.50
    }

    #endregion
}
