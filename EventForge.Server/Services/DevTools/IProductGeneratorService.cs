using EventForge.DTOs.DevTools;

namespace EventForge.Server.Services.DevTools;

/// <summary>
/// Servizio per la generazione di prodotti di test.
/// </summary>
public interface IProductGeneratorService
{
    /// <summary>
    /// Avvia un job asincrono per generare prodotti di test.
    /// </summary>
    /// <param name="request">Parametri di generazione</param>
    /// <param name="tenantId">ID del tenant per cui generare i prodotti</param>
    /// <param name="userId">ID dell'utente che ha avviato il job</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>ID del job avviato</returns>
    Task<string> StartGenerationJobAsync(GenerateProductsRequestDto request, Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene lo stato corrente di un job di generazione.
    /// </summary>
    /// <param name="jobId">ID del job</param>
    /// <returns>Stato del job o null se non trovato</returns>
    GenerateProductsStatusDto? GetJobStatus(string jobId);

    /// <summary>
    /// Cancella un job in esecuzione.
    /// </summary>
    /// <param name="jobId">ID del job da cancellare</param>
    /// <returns>True se il job è stato cancellato, false se non è stato trovato o non può essere cancellato</returns>
    bool CancelJob(string jobId);
}
