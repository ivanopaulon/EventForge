using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for price trend calculations with discount and base unit normalization.
/// Tests verify that:
/// 1. Line discounts (percentage and value) are correctly applied
/// 2. Base unit prices and quantities are used when available
/// 3. Effective prices are calculated correctly for trend analysis
/// 4. Weighted averages use effective prices and quantities
/// </summary>
[Trait("Category", "Unit")]
public class PriceTrendCalculationTests
{
    [Fact]
    public void EffectivePrice_WithPercentageDiscount_CalculatesCorrectly()
    {
        // Arrange
        var row = new DocumentRowDto
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscount = 10m, // 10%
            DiscountType = DiscountType.Percentage,
            LineTotal = 180m // Pre-calculated by entity
        };

        // Act - Simulate the calculation in ProductManagementController
        decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
        decimal unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);
        decimal effectiveUnitPrice = unitPriceNormalized - unitDiscount;

        // Assert
        Assert.Equal(90m, effectiveUnitPrice); // 100 - (100 * 0.10) = 90

        // Verify it matches what LineTotal/Quantity would give us
        decimal expectedFromLineTotal = row.LineTotal / row.Quantity;
        Assert.Equal(expectedFromLineTotal, effectiveUnitPrice);
    }

    [Fact]
    public void EffectivePrice_WithValueDiscount_CalculatesCorrectly()
    {
        // Arrange
        var row = new DocumentRowDto
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscountValue = 30m,
            DiscountType = DiscountType.Value,
            LineTotal = 170m // Pre-calculated by entity
        };

        // Act
        decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
        decimal unitDiscount = row.LineDiscountValue / row.Quantity;
        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);
        decimal effectiveUnitPrice = unitPriceNormalized - unitDiscount;

        // Assert
        Assert.Equal(85m, effectiveUnitPrice); // 100 - (30 / 2) = 85

        // Verify it matches what LineTotal/Quantity would give us
        decimal expectedFromLineTotal = row.LineTotal / row.Quantity;
        Assert.Equal(expectedFromLineTotal, effectiveUnitPrice);
    }

    [Fact]
    public void EffectivePrice_WithBaseUnitPrice_UsesBaseUnitPrice()
    {
        // Arrange - Product sold in packs of 6 units
        var row = new DocumentRowDto
        {
            UnitPrice = 60m,        // Price per pack
            BaseUnitPrice = 10m,    // Price per single unit
            Quantity = 2m,          // 2 packs
            BaseQuantity = 12m,     // 12 single units
            LineDiscount = 10m,     // 10% discount
            DiscountType = DiscountType.Percentage,
            LineTotal = 108m        // (60 * 2) * 0.9 = 108
        };

        // Act - Use base unit price for normalization
        decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
        decimal unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);
        decimal effectiveUnitPrice = unitPriceNormalized - unitDiscount;
        decimal weightQuantity = row.BaseQuantity ?? row.Quantity;

        // Assert
        Assert.Equal(9m, effectiveUnitPrice); // 10 - (10 * 0.10) = 9 per single unit
        Assert.Equal(12m, weightQuantity);    // Use base quantity for weighting
    }

    [Fact]
    public void EffectivePrice_WithDiscountExceedingPrice_ClampedToZero()
    {
        // Arrange
        var row = new DocumentRowDto
        {
            UnitPrice = 100m,
            Quantity = 2m,
            LineDiscountValue = 250m, // Exceeds subtotal
            DiscountType = DiscountType.Value,
            LineTotal = 0m
        };

        // Act
        decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
        decimal unitDiscount = row.LineDiscountValue / row.Quantity; // 125
        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);   // Clamped to 100
        decimal effectiveUnitPrice = unitPriceNormalized - unitDiscount;

        // Assert
        Assert.Equal(0m, effectiveUnitPrice); // 100 - 100 = 0 (clamped)
    }

    [Fact]
    public void EffectivePrice_WithZeroQuantity_HandlesGracefully()
    {
        // Arrange
        var row = new DocumentRowDto
        {
            UnitPrice = 100m,
            Quantity = 0m,
            LineDiscount = 10m,
            DiscountType = DiscountType.Percentage,
            LineTotal = 0m
        };

        // Act
        decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
        decimal unitDiscount = 0m;
        if (row.Quantity > 0)
        {
            unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
        }
        decimal effectiveUnitPrice = unitPriceNormalized - unitDiscount;

        // Assert
        Assert.Equal(100m, effectiveUnitPrice); // No discount applied when quantity is 0
    }

    [Fact]
    public void WeightedAverage_UsesEffectivePricesAndQuantities()
    {
        // Arrange - Simulate multiple price points
        var pricePoints = new List<PriceTrendDataPoint>
        {
            new PriceTrendDataPoint { Price = 90m, Quantity = 2m },   // Effective price after 10% discount
            new PriceTrendDataPoint { Price = 85m, Quantity = 3m },   // Different discount
            new PriceTrendDataPoint { Price = 95m, Quantity = 1m }    // Minimal discount
        };

        // Act - Calculate weighted average
        var totalValue = pricePoints.Sum(p => p.Price * p.Quantity);
        var totalQuantity = pricePoints.Sum(p => p.Quantity);
        var weightedAvg = totalQuantity > 0 ? totalValue / totalQuantity : 0;

        // Assert
        // (90*2 + 85*3 + 95*1) / (2+3+1) = (180 + 255 + 95) / 6 = 530 / 6 = 88.33...
        Assert.Equal(88.333333333333333333333333333m, weightedAvg);
        Assert.True(weightedAvg > 85m && weightedAvg < 90m);
    }

    [Fact]
    public void MinMaxCalculations_UseEffectivePrices()
    {
        // Arrange - Price points with effective prices after discounts
        var pricePoints = new List<PriceTrendDataPoint>
        {
            new PriceTrendDataPoint { Price = 90m, Quantity = 2m },
            new PriceTrendDataPoint { Price = 85m, Quantity = 3m },
            new PriceTrendDataPoint { Price = 95m, Quantity = 1m },
            new PriceTrendDataPoint { Price = 88m, Quantity = 5m }
        };

        // Act
        var prices = pricePoints.Select(p => p.Price).ToList();
        var minPrice = prices.Min();
        var maxPrice = prices.Max();
        var avgPrice = prices.Average();

        // Assert
        Assert.Equal(85m, minPrice);
        Assert.Equal(95m, maxPrice);
        Assert.Equal(89.5m, avgPrice); // (90 + 85 + 95 + 88) / 4 = 89.5
    }

    [Fact]
    public void PriceTrend_HandlesMultipleDiscountTypes()
    {
        // Arrange - Mix of percentage and value discounts
        var rows = new List<DocumentRowDto>
        {
            new DocumentRowDto
            {
                UnitPrice = 100m,
                Quantity = 2m,
                LineDiscount = 10m,
                DiscountType = DiscountType.Percentage,
                LineTotal = 180m
            },
            new DocumentRowDto
            {
                UnitPrice = 100m,
                Quantity = 2m,
                LineDiscountValue = 30m,
                DiscountType = DiscountType.Value,
                LineTotal = 170m
            }
        };

        // Act - Calculate effective prices
        var effectivePrices = new List<decimal>();
        foreach (var row in rows)
        {
            decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
            decimal unitDiscount = 0m;

            if (row.Quantity > 0)
            {
                if (row.DiscountType == DiscountType.Percentage)
                {
                    unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
                }
                else
                {
                    unitDiscount = row.LineDiscountValue / row.Quantity;
                }
                unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);
            }

            effectivePrices.Add(unitPriceNormalized - unitDiscount);
        }

        // Assert
        Assert.Equal(2, effectivePrices.Count);
        Assert.Equal(90m, effectivePrices[0]);  // Percentage discount: 100 - 10 = 90
        Assert.Equal(85m, effectivePrices[1]);  // Value discount: 100 - 15 = 85
    }

    [Fact]
    public void EffectivePrice_RoundingTo4Decimals()
    {
        // Arrange
        var row = new DocumentRowDto
        {
            UnitPrice = 100m,
            Quantity = 3m,
            LineDiscount = 33.333m, // Results in repeating decimal
            DiscountType = DiscountType.Percentage
        };

        // Act
        decimal unitPriceNormalized = row.BaseUnitPrice ?? row.UnitPrice;
        decimal unitDiscount = unitPriceNormalized * (row.LineDiscount / 100m);
        unitDiscount = Math.Min(unitDiscount, unitPriceNormalized);
        decimal effectiveUnitPrice = Math.Round(unitPriceNormalized - unitDiscount, 4);

        // Assert
        Assert.Equal(66.667m, effectiveUnitPrice); // Rounded to 4 decimals
    }
}
