namespace EventForge.Server.Services.UnitOfMeasures;

/// <summary>
/// Service for unit of measure conversions with proper rounding using MidpointRounding.AwayFromZero.
/// Implements Issue #244 requirements for decimal conversion factors and "away from zero" rounding.
/// </summary>
public class UnitConversionService : IUnitConversionService
{
    /// <summary>
    /// Converts a quantity from one unit to another using conversion factors.
    /// Uses MidpointRounding.AwayFromZero for consistent rounding behavior.
    /// </summary>
    /// <param name="quantity">The quantity to convert</param>
    /// <param name="fromConversionFactor">Conversion factor of the source unit</param>
    /// <param name="toConversionFactor">Conversion factor of the target unit</param>
    /// <param name="decimalPlaces">Number of decimal places to round to (default: 2)</param>
    /// <returns>Converted quantity rounded using AwayFromZero strategy</returns>
    public decimal ConvertQuantity(decimal quantity, decimal fromConversionFactor, decimal toConversionFactor, int decimalPlaces = 2)
    {
        if (!IsValidConversionFactor(fromConversionFactor))
            throw new ArgumentException("Source conversion factor must be greater than zero.", nameof(fromConversionFactor));

        if (!IsValidConversionFactor(toConversionFactor))
            throw new ArgumentException("Target conversion factor must be greater than zero.", nameof(toConversionFactor));

        if (decimalPlaces < 0)
            throw new ArgumentException("Decimal places must be non-negative.", nameof(decimalPlaces));

        // Convert to base units first, then to target units
        decimal baseQuantity = quantity * fromConversionFactor;
        decimal targetQuantity = baseQuantity / toConversionFactor;

        // Apply AwayFromZero rounding as specified in Issue #244
        return Math.Round(targetQuantity, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Converts a quantity to the base unit (factor 1.0).
    /// </summary>
    /// <param name="quantity">The quantity to convert</param>
    /// <param name="conversionFactor">Conversion factor of the source unit</param>
    /// <param name="decimalPlaces">Number of decimal places to round to (default: 2)</param>
    /// <returns>Quantity in base units</returns>
    public decimal ConvertToBaseUnit(decimal quantity, decimal conversionFactor, int decimalPlaces = 2)
    {
        if (!IsValidConversionFactor(conversionFactor))
            throw new ArgumentException("Conversion factor must be greater than zero.", nameof(conversionFactor));

        if (decimalPlaces < 0)
            throw new ArgumentException("Decimal places must be non-negative.", nameof(decimalPlaces));

        decimal baseQuantity = quantity * conversionFactor;
        return Math.Round(baseQuantity, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Converts a quantity from the base unit to the specified unit.
    /// </summary>
    /// <param name="baseQuantity">The quantity in base units</param>
    /// <param name="conversionFactor">Conversion factor of the target unit</param>
    /// <param name="decimalPlaces">Number of decimal places to round to (default: 2)</param>
    /// <returns>Quantity in the target unit</returns>
    public decimal ConvertFromBaseUnit(decimal baseQuantity, decimal conversionFactor, int decimalPlaces = 2)
    {
        if (!IsValidConversionFactor(conversionFactor))
            throw new ArgumentException("Conversion factor must be greater than zero.", nameof(conversionFactor));

        if (decimalPlaces < 0)
            throw new ArgumentException("Decimal places must be non-negative.", nameof(decimalPlaces));

        decimal targetQuantity = baseQuantity / conversionFactor;
        return Math.Round(targetQuantity, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Validates that a conversion factor is valid.
    /// </summary>
    /// <param name="conversionFactor">The conversion factor to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidConversionFactor(decimal conversionFactor)
    {
        return conversionFactor > 0m;
    }

    /// <summary>
    /// Calculates the price per unit when converting between different units.
    /// </summary>
    /// <param name="price">Original price</param>
    /// <param name="fromConversionFactor">Conversion factor of the price unit</param>
    /// <param name="toConversionFactor">Conversion factor of the target unit</param>
    /// <param name="decimalPlaces">Number of decimal places to round to (default: 2)</param>
    /// <returns>Price adjusted for the target unit</returns>
    public decimal ConvertPrice(decimal price, decimal fromConversionFactor, decimal toConversionFactor, int decimalPlaces = 2)
    {
        if (!IsValidConversionFactor(fromConversionFactor))
            throw new ArgumentException("Source conversion factor must be greater than zero.", nameof(fromConversionFactor));

        if (!IsValidConversionFactor(toConversionFactor))
            throw new ArgumentException("Target conversion factor must be greater than zero.", nameof(toConversionFactor));

        if (decimalPlaces < 0)
            throw new ArgumentException("Decimal places must be non-negative.", nameof(decimalPlaces));

        // Price conversion is inverse of quantity conversion
        // If unit A has factor 2 and unit B has factor 4:
        // - 1 unit A = 2 base units
        // - 1 unit B = 4 base units  
        // - So 1 unit B = 2 units A
        // - Therefore price per unit A = price per unit B * 2
        decimal priceRatio = toConversionFactor / fromConversionFactor;
        decimal convertedPrice = price * priceRatio;

        return Math.Round(convertedPrice, decimalPlaces, MidpointRounding.AwayFromZero);
    }
}