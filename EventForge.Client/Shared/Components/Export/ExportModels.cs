namespace EventForge.Client.Shared.Components.Export;

/// <summary>
/// Configurazione di una colonna per l'export.
/// </summary>
public class ExportColumnConfig
{
    /// <summary>
    /// Nome della proprietà dell'entità.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome visualizzato nell'header.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Se true, questa colonna verrà inclusa nell'export.
    /// </summary>
    public bool IncludeInExport { get; set; } = true;
    
    /// <summary>
    /// Formato numerico per Excel (es. "€#,##0.00", "dd/mm/yyyy").
    /// </summary>
    public string? NumberFormat { get; set; }
    
    /// <summary>
    /// Ordine di visualizzazione.
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// Formati di export supportati.
/// </summary>
public enum ExportFormat
{
    Excel,
    Csv
}

/// <summary>
/// Risultato del dialog di configurazione export.
/// </summary>
public class ExportDialogResult
{
    public ExportFormat Format { get; set; }
    public List<ExportColumnConfig> Columns { get; set; } = new();
}

/// <summary>
/// Richiesta di export da inviare al service/handler.
/// </summary>
public class ExportRequest
{
    public List<ExportColumnConfig> Columns { get; set; } = new();
    public ExportFormat Format { get; set; }
    public string FileName { get; set; } = "Export";
}
