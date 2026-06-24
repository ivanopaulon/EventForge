using System.Globalization;

namespace Prym.DTOs.Documents;

/// <summary>
/// Result of parsing a chained discount string (e.g. "10+5", "10+5+2", "15").
/// </summary>
public sealed class DiscountParseResult
{
    /// <summary>Whether the input was valid and could be parsed.</summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The equivalent single percentage calculated from the cascade of discounts.
    /// Formula: 100 - ∏(1 - d_i/100) × 100
    /// </summary>
    public decimal EquivalentPercentage { get; init; }

    /// <summary>The individual discount parts parsed from the input (e.g. [10, 5]).</summary>
    public decimal[] Parts { get; init; } = Array.Empty<decimal>();

    /// <summary>True when the input contained a '+' separator (i.e. is a genuine cascade).</summary>
    public bool IsChained => Parts.Length > 1;

    /// <summary>Human-readable error message when <see cref="IsValid"/> is false.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Parses and validates chained percentage discount strings of the form "10+5", "10+5+2", "15", etc.
/// <para>
/// A chained discount means each component is applied sequentially to the remaining amount,
/// not summed. For example "10+5" = 1 - (0.90 × 0.95) = 14.5%, not 15%.
/// </para>
/// </summary>
public static class DiscountStringParser
{
    /// <summary>
    /// Pattern description for documentation / error messages.
    /// Each '+'-separated token must be a non-negative decimal between 0 and 100.
    /// </summary>
    public const string PatternDescription = "Formato atteso: numero o numeri separati da '+' (es. '10', '10+5', '10+5+2')";

    /// <summary>
    /// Parses the given discount string.
    /// Returns a valid result for single numeric values as well as chained "10+5" style strings.
    /// </summary>
    /// <param name="input">Raw user input (may contain spaces, commas, etc.).</param>
    /// <returns>A <see cref="DiscountParseResult"/> describing the outcome.</returns>
    public static DiscountParseResult Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new DiscountParseResult
            {
                IsValid = false,
                ErrorMessage = "Il valore dello sconto non può essere vuoto."
            };
        }

        // Normalise: trim, replace comma with dot for decimal separator, collapse spaces
        var normalised = input.Trim().Replace(',', '.').Replace(" ", "");

        // Guard against malformed sequences like "10++5" or "+10"
        if (normalised.StartsWith('+') || normalised.EndsWith('+') || normalised.Contains("++"))
        {
            return new DiscountParseResult
            {
                IsValid = false,
                ErrorMessage = $"Formato non valido: '{input}'. {PatternDescription}."
            };
        }

        var tokens = normalised.Split('+');
        var parts = new decimal[tokens.Length];

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];

            if (!decimal.TryParse(token, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            {
                return new DiscountParseResult
                {
                    IsValid = false,
                    ErrorMessage = $"'{token}' non è un numero valido. {PatternDescription}."
                };
            }

            if (value < 0 || value > 100)
            {
                return new DiscountParseResult
                {
                    IsValid = false,
                    ErrorMessage = $"Ogni componente dello sconto deve essere compreso tra 0 e 100 (trovato: {value})."
                };
            }

            parts[i] = value;
        }

        // Calculate cascaded equivalent: 100 - ∏(1 - d_i/100) * 100
        var remaining = 1m;
        foreach (var part in parts)
        {
            remaining *= 1m - (part / 100m);
        }
        var equivalent = Math.Round((1m - remaining) * 100m, 10, MidpointRounding.AwayFromZero);

        // Round to 6 decimal places for storage/display
        equivalent = Math.Round(equivalent, 6, MidpointRounding.AwayFromZero);

        return new DiscountParseResult
        {
            IsValid = true,
            EquivalentPercentage = equivalent,
            Parts = parts
        };
    }

    /// <summary>
    /// Returns true if the string is null/empty (meaning no chained discount, use LineDiscount directly).
    /// </summary>
    public static bool IsEmpty(string? input) => string.IsNullOrWhiteSpace(input);

    /// <summary>
    /// Returns true when the input is a plain single number (no '+') — in this case
    /// LineDiscountString should be stored as null to avoid redundancy.
    /// </summary>
    public static bool IsSingleValue(string? input)
    {
        if (IsEmpty(input)) return false;
        return !input!.Contains('+');
    }
}
