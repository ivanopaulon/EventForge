namespace Prym.DTOs.Constants;

public static class SaleTypes
{
    public const string Retail = "RETAIL";
    public const string Wholesale = "WHOLESALE";
    public const string Online = "ONLINE";
    public const string Takeaway = "TAKEAWAY";
}

public static class Currencies
{
    public const string EUR = "EUR";
    public const string USD = "USD";
    public const string GBP = "GBP";
}

/// <summary>Valori stringa dello stato tavolo usati da TableSessionDto/UpdateTableStatusDto.
/// Non è un enum lato DTO/entità (campo string libero); questa classe evita typo nei
/// confronti/assegnazioni sparsi nel codice consumer (es. POS2026.razor.cs).</summary>
public static class TableStatuses
{
    public const string Available = "Available";
    public const string Occupied = "Occupied";
}
