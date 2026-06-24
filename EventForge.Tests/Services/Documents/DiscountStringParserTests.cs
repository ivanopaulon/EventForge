using Prym.DTOs.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for <see cref="DiscountStringParser"/>.
/// Covers single values, chained discounts, edge cases, and invalid inputs.
/// </summary>
[Trait("Category", "Unit")]
public class DiscountStringParserTests
{
    #region Single-value inputs

    [Theory]
    [InlineData("0")]
    [InlineData("0.0")]
    public void Parse_ZeroDiscount_IsValidWithZeroEquivalent(string input)
    {
        var result = DiscountStringParser.Parse(input);

        Assert.True(result.IsValid);
        Assert.Equal(0m, result.EquivalentPercentage);
        Assert.Single(result.Parts);
        Assert.False(result.IsChained);
    }

    [Fact]
    public void Parse_SingleValue_ReturnsCorrectEquivalent()
    {
        var result = DiscountStringParser.Parse("15");

        Assert.True(result.IsValid);
        Assert.Equal(15m, result.EquivalentPercentage);
        Assert.Single(result.Parts);
        Assert.Equal(15m, result.Parts[0]);
        Assert.False(result.IsChained);
    }

    [Fact]
    public void Parse_SingleDecimalValue_ReturnsCorrectEquivalent()
    {
        var result = DiscountStringParser.Parse("10.5");

        Assert.True(result.IsValid);
        Assert.Equal(10.5m, result.EquivalentPercentage);
        Assert.Single(result.Parts);
        Assert.False(result.IsChained);
    }

    [Fact]
    public void Parse_HundredPercent_IsValidFullDiscount()
    {
        var result = DiscountStringParser.Parse("100");

        Assert.True(result.IsValid);
        Assert.Equal(100m, result.EquivalentPercentage);
        Assert.False(result.IsChained);
    }

    #endregion

    #region Chained discount inputs

    [Fact]
    public void Parse_TenPlusFive_Returns14Point5Percent()
    {
        // 1 - (0.90 × 0.95) = 0.145 = 14.5%
        var result = DiscountStringParser.Parse("10+5");

        Assert.True(result.IsValid);
        Assert.Equal(14.5m, result.EquivalentPercentage);
        Assert.Equal(2, result.Parts.Length);
        Assert.Equal(10m, result.Parts[0]);
        Assert.Equal(5m, result.Parts[1]);
        Assert.True(result.IsChained);
    }

    [Fact]
    public void Parse_TenPlusFivePlusTwo_ReturnsCorrectEquivalent()
    {
        // 1 - (0.90 × 0.95 × 0.98) = 1 - 0.83790 = 0.1621 = 16.21%
        var result = DiscountStringParser.Parse("10+5+2");

        Assert.True(result.IsValid);
        Assert.Equal(3, result.Parts.Length);
        Assert.True(result.IsChained);

        // Verify formula: 100 - (90 * 95 * 98) / 10000
        var expected = 100m - (0.90m * 0.95m * 0.98m) * 100m;
        expected = Math.Round(expected, 6, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, result.EquivalentPercentage);
    }

    [Fact]
    public void Parse_WithSpaces_TrimsAndParses()
    {
        var result = DiscountStringParser.Parse("  10+5  ");

        Assert.True(result.IsValid);
        Assert.Equal(14.5m, result.EquivalentPercentage);
    }

    [Fact]
    public void Parse_WithCommaDecimalSeparator_Parses()
    {
        // "10,5+5" → 10.5 + 5 chained
        var result = DiscountStringParser.Parse("10,5+5");

        Assert.True(result.IsValid);
        Assert.True(result.IsChained);
        Assert.Equal(10.5m, result.Parts[0]);
        Assert.Equal(5m, result.Parts[1]);
    }

    [Fact]
    public void Parse_TwentyPlusZero_EquivalentIsTwenty()
    {
        // Cascading 20% + 0% is still 20%
        var result = DiscountStringParser.Parse("20+0");

        Assert.True(result.IsValid);
        Assert.Equal(20m, result.EquivalentPercentage);
    }

    [Fact]
    public void Parse_HundredPlusFive_IsValid_FullDiscountResult()
    {
        // 100% first step means nothing remains for subsequent steps
        var result = DiscountStringParser.Parse("100+5");

        Assert.True(result.IsValid);
        Assert.Equal(100m, result.EquivalentPercentage);
    }

    #endregion

    #region Invalid inputs

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhitespace_IsInvalid(string? input)
    {
        var result = DiscountStringParser.Parse(input);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_Alphabetic_IsInvalid()
    {
        var result = DiscountStringParser.Parse("abc");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_ValueOver100_IsInvalid()
    {
        var result = DiscountStringParser.Parse("110");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_NegativeValue_IsInvalid()
    {
        var result = DiscountStringParser.Parse("-5");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_DoubledPlus_IsInvalid()
    {
        var result = DiscountStringParser.Parse("10++5");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_LeadingPlus_IsInvalid()
    {
        var result = DiscountStringParser.Parse("+10");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_TrailingPlus_IsInvalid()
    {
        var result = DiscountStringParser.Parse("10+");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_ChainWithInvalidPart_IsInvalid()
    {
        var result = DiscountStringParser.Parse("10+abc+5");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_ChainWithOver100Part_IsInvalid()
    {
        var result = DiscountStringParser.Parse("10+110");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion

    #region Helper method tests

    [Fact]
    public void IsEmpty_NullInput_ReturnsTrue()
    {
        Assert.True(DiscountStringParser.IsEmpty(null));
    }

    [Fact]
    public void IsEmpty_WhitespaceInput_ReturnsTrue()
    {
        Assert.True(DiscountStringParser.IsEmpty("   "));
    }

    [Fact]
    public void IsEmpty_ValidInput_ReturnsFalse()
    {
        Assert.False(DiscountStringParser.IsEmpty("10+5"));
    }

    [Fact]
    public void IsSingleValue_NoPlusSign_ReturnsTrue()
    {
        Assert.True(DiscountStringParser.IsSingleValue("15"));
    }

    [Fact]
    public void IsSingleValue_WithPlusSign_ReturnsFalse()
    {
        Assert.False(DiscountStringParser.IsSingleValue("10+5"));
    }

    [Fact]
    public void IsSingleValue_NullInput_ReturnsFalse()
    {
        Assert.False(DiscountStringParser.IsSingleValue(null));
    }

    #endregion
}
