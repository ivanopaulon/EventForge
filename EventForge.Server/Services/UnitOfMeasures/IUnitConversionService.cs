using EventForge.DTOs.Products;

namespace EventForge.Server.Services.UnitOfMeasures;

/// <summary>
/// Service interface for unit of measure conversions with proper rounding.
/// </summary>
public interface IUnitConversionService
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
    decimal ConvertQuantity(decimal quantity, decimal fromConversionFactor, decimal toConversionFactor, int decimalPlaces = 2);

    /// <summary>
    /// Converts a quantity to the base unit (factor 1.0).
    /// </summary>
    /// <param name="quantity">The quantity to convert</param>
    /// <param name="conversionFactor">Conversion factor of the source unit</param>
    /// <param name="decimalPlaces">Number of decimal places to round to (default: 2)</param>
    /// <returns>Quantity in base units</returns>
    decimal ConvertToBaseUnit(decimal quantity, decimal conversionFactor, int decimalPlaces = 2);

    /// <summary>
    /// Converts a quantity from the base unit to the specified unit.
    /// </summary>
    /// <param name="baseQuantity">The quantity in base units</param>
    /// <param name="conversionFactor">Conversion factor of the target unit</param>
    /// <param name="decimalPlaces">Number of decimal places to round to (default: 2)</param>
    /// <returns>Quantity in the target unit</returns>
    decimal ConvertFromBaseUnit(decimal baseQuantity, decimal conversionFactor, int decimalPlaces = 2);

    /// <summary>
    /// Validates that a conversion factor is valid.
    /// </summary>
    /// <param name="conversionFactor">The conversion factor to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidConversionFactor(decimal conversionFactor);

    /// <summary>
    /// Calculates the price per unit when converting between different units.
    /// </summary>
    /// <param name="price">Original price</param>
    /// <param name="fromConversionFactor">Conversion factor of the price unit</param>
    /// <param name="toConversionFactor">Conversion factor of the target unit</param>
    /// <param name="decimalPlaces">Number of decimal places to round to (default: 2)</param>
    /// <returns>Price adjusted for the target unit</returns>
    decimal ConvertPrice(decimal price, decimal fromConversionFactor, decimal toConversionFactor, int decimalPlaces = 2);
}