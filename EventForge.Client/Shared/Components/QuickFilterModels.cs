using MudBlazor;

namespace EventForge.Client.Shared.Components;

/// <summary>
/// Rappresenta un filtro rapido (quick filter) per EFTable.
/// </summary>
/// <typeparam name="TItem">Il tipo di elemento da filtrare.</typeparam>
public class QuickFilter<TItem>
{
    /// <summary>
    /// Identificatore univoco del filtro.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Etichetta visualizzata nel chip.
    /// </summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Predicato per filtrare gli elementi.
    /// </summary>
    public Func<TItem, bool>? Predicate { get; set; }
    
    /// <summary>
    /// Colore del chip (Default, Primary, Secondary, Success, Warning, Error, Info).
    /// </summary>
    public Color Color { get; set; } = Color.Default;
    
    /// <summary>
    /// Icona opzionale da mostrare nel chip.
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Descrizione estesa del filtro (tooltip).
    /// </summary>
    public string? Description { get; set; }
}
