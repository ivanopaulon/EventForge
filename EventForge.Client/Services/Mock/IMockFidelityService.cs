using EventForge.Client.Models.Fidelity;

namespace EventForge.Client.Services.Mock;

/// <summary>
/// Interface per il servizio mock di gestione carte fedeltà
/// </summary>
public interface IMockFidelityService
{
    /// <summary>
    /// Ottiene tutte le carte fedeltà
    /// </summary>
    Task<IEnumerable<FidelityCardViewModel>> GetAllCardsAsync();
    
    /// <summary>
    /// Ottiene una carta fedeltà per ID
    /// </summary>
    Task<FidelityCardViewModel?> GetCardByIdAsync(Guid id);
    
    /// <summary>
    /// Crea una nuova carta fedeltà
    /// </summary>
    Task<FidelityCardViewModel> CreateCardAsync(FidelityCardViewModel card);
    
    /// <summary>
    /// Aggiorna una carta fedeltà esistente
    /// </summary>
    Task<FidelityCardViewModel> UpdateCardAsync(FidelityCardViewModel card);
    
    /// <summary>
    /// Revoca una carta fedeltà
    /// </summary>
    Task RevokeCardAsync(Guid cardId);
    
    /// <summary>
    /// Sospende una carta fedeltà
    /// </summary>
    Task SuspendCardAsync(Guid cardId);
    
    /// <summary>
    /// Riattiva una carta fedeltà sospesa
    /// </summary>
    Task ActivateCardAsync(Guid cardId);
    
    /// <summary>
    /// Aggiunge punti a una carta fedeltà
    /// </summary>
    Task AddPointsAsync(Guid cardId, int points, string description);
    
    /// <summary>
    /// Rimuove punti da una carta fedeltà
    /// </summary>
    Task RedeemPointsAsync(Guid cardId, int points, string description);
    
    /// <summary>
    /// Ottiene lo storico transazioni di una carta
    /// </summary>
    Task<IEnumerable<FidelityPointsTransactionViewModel>> GetTransactionHistoryAsync(Guid cardId);
}
