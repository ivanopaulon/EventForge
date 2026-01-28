namespace EventForge.Client.Pages.Management.Business;

/// <summary>
/// Enum per lo stato di caricamento delle tab.
/// Utilizzato per tracciare se i dati di una tab sono stati caricati o meno.
/// </summary>
public enum TabLoadState
{
    /// <summary>
    /// Stato iniziale - i dati non sono ancora stati caricati.
    /// </summary>
    NotLoaded,
    
    /// <summary>
    /// Caricamento in corso - fetch dei dati in progress.
    /// </summary>
    Loading,
    
    /// <summary>
    /// Dati caricati con successo.
    /// </summary>
    Loaded,
    
    /// <summary>
    /// Errore durante il caricamento dei dati.
    /// </summary>
    Error
}
