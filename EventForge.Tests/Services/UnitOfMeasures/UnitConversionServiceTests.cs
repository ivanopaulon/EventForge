using EventForge.Server.Services.UnitOfMeasures;

namespace EventForge.Tests.Services.UnitOfMeasures;

/// <summary>
/// Unit tests for UnitConversionService implementation (Issue #244).
/// Tests decimal conversion factors and "away from zero" rounding behavior.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Unit")]
public class UnitConversionServiceTests
{
    private readonly IUnitConversionService _conversionService;

    public UnitConversionServiceTests()
    {
        _conversionService = new UnitConversionService();
    }

    [Fact]
    public void ConvertQuantity_ShouldConvertCorrectly_WhenFactorsAreValid()
    {
        // Arrange
        decimal quantity = 10m;
        decimal fromFactor = 2.5m; // Source unit: 1 unit = 2.5 base units
        decimal toFactor = 5m;     // Target unit: 1 unit = 5 base units

        // Act
        decimal result = _conversionService.ConvertQuantity(quantity, fromFactor, toFactor);

        // Assert
        // 10 * 2.5 = 25 base units
        // 25 / 5 = 5 target units
        Assert.Equal(5m, result);
    }

    [Fact]
    public void ConvertQuantity_ShouldUseAwayFromZeroRounding_WhenResultHasDecimals()
    {
        // Arrange
        decimal quantity = 3m;
        decimal fromFactor = 1m;
        decimal toFactor = 7m;

        // Act
        decimal result = _conversionService.ConvertQuantity(quantity, fromFactor, toFactor, 2);

        // Assert
        // 3 / 7 = 0.428571... rounded to 2 decimal places with AwayFromZero = 0.43
        Assert.Equal(0.43m, result);
    }

    [Fact]
    public void ConvertQuantity_ShouldRoundMidpointAwayFromZero_WhenResultIsExactMidpoint()
    {
        // Arrange
        decimal quantity = 2.5m;
        decimal fromFactor = 1m;
        decimal toFactor = 1m;

        // Act
        decimal result = _conversionService.ConvertQuantity(quantity, fromFactor, toFactor, 0);

        // Assert
        // 2.5 rounded to 0 decimal places with AwayFromZero = 3 (not 2)
        Assert.Equal(3m, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void ConvertQuantity_ShouldThrowArgumentException_WhenFromFactorIsInvalid(decimal invalidFactor)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _conversionService.ConvertQuantity(10m, invalidFactor, 1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.5)]
    public void ConvertQuantity_ShouldThrowArgumentException_WhenToFactorIsInvalid(decimal invalidFactor)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _conversionService.ConvertQuantity(10m, 1m, invalidFactor));
    }

    [Fact]
    public void ConvertToBaseUnit_ShouldMultiplyByFactor()
    {
        // Arrange
        decimal quantity = 6m;
        decimal conversionFactor = 2.5m;

        // Act
        decimal result = _conversionService.ConvertToBaseUnit(quantity, conversionFactor);

        // Assert
        // 6 * 2.5 = 15 base units
        Assert.Equal(15m, result);
    }

    [Fact]
    public void ConvertFromBaseUnit_ShouldDivideByFactor()
    {
        // Arrange
        decimal baseQuantity = 15m;
        decimal conversionFactor = 2.5m;

        // Act
        decimal result = _conversionService.ConvertFromBaseUnit(baseQuantity, conversionFactor);

        // Assert
        // 15 / 2.5 = 6 units
        Assert.Equal(6m, result);
    }

    [Fact]
    public void ConvertPrice_ShouldConvertCorrectly_WhenFactorsAreValid()
    {
        // Arrange
        decimal price = 10m; // Price for source unit
        decimal fromFactor = 1m;  // Source unit: 1 unit = 1 base unit
        decimal toFactor = 5m;    // Target unit: 1 unit = 5 base units

        // Act
        decimal result = _conversionService.ConvertPrice(price, fromFactor, toFactor);

        // Assert
        // If 1 source unit costs 10 and 1 target unit = 5 source units
        // Then 1 target unit should cost 10 * 5 = 50
        Assert.Equal(50m, result);
    }

    [Fact]
    public void ConvertPrice_ShouldHandleDecimalFactors()
    {
        // Arrange
        decimal price = 20m;      // Price for source unit
        decimal fromFactor = 2.5m; // Source unit: 1 unit = 2.5 base units
        decimal toFactor = 1.25m;  // Target unit: 1 unit = 1.25 base units

        // Act
        decimal result = _conversionService.ConvertPrice(price, fromFactor, toFactor);

        // Assert
        // Price ratio = 1.25 / 2.5 = 0.5
        // New price = 20 * 0.5 = 10
        Assert.Equal(10m, result);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1)]
    [InlineData(10.5)]
    [InlineData(1000)]
    public void IsValidConversionFactor_ShouldReturnTrue_WhenFactorIsPositive(decimal factor)
    {
        // Act
        bool result = _conversionService.IsValidConversionFactor(factor);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(-1)]
    [InlineData(-10.5)]
    public void IsValidConversionFactor_ShouldReturnFalse_WhenFactorIsZeroOrNegative(decimal factor)
    {
        // Act
        bool result = _conversionService.IsValidConversionFactor(factor);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ConvertQuantity_ShouldHandleComplexDecimalScenario()
    {
        // Arrange - Real world scenario
        decimal quantity = 3.75m;    // 3.75 packs
        decimal fromFactor = 6.5m;   // Pack contains 6.5 pieces
        decimal toFactor = 1m;       // Convert to individual pieces

        // Act
        decimal result = _conversionService.ConvertQuantity(quantity, fromFactor, toFactor);

        // Assert
        // 3.75 * 6.5 = 24.375 pieces, rounded to 24.38
        Assert.Equal(24.38m, result);
    }
}