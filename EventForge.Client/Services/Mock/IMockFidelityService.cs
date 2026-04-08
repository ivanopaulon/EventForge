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
    Task<IEnumerable<FidelityCardViewModel>> GetAllCardsAsync(CancellationToken ct = default);

    /// <summary>
    /// Ottiene una carta fedeltà per ID
    /// </summary>
    Task<FidelityCardViewModel?> GetCardByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Crea una nuova carta fedeltà
    /// </summary>
    Task<FidelityCardViewModel> CreateCardAsync(FidelityCardViewModel card, CancellationToken ct = default);

    /// <summary>
    /// Aggiorna una carta fedeltà esistente
    /// </summary>
    Task<FidelityCardViewModel> UpdateCardAsync(FidelityCardViewModel card, CancellationToken ct = default);

    /// <summary>
    /// Revoca una carta fedeltà
    /// </summary>
    Task RevokeCardAsync(Guid cardId, CancellationToken ct = default);

    /// <summary>
    /// Sospende una carta fedeltà
    /// </summary>
    Task SuspendCardAsync(Guid cardId, CancellationToken ct = default);

    /// <summary>
    /// Riattiva una carta fedeltà sospesa
    /// </summary>
    Task ActivateCardAsync(Guid cardId, CancellationToken ct = default);

    /// <summary>
    /// Aggiunge punti a una carta fedeltà
    /// </summary>
    Task AddPointsAsync(Guid cardId, int points, string description, CancellationToken ct = default);

    /// <summary>
    /// Rimuove punti da una carta fedeltà
    /// </summary>
    Task RedeemPointsAsync(Guid cardId, int points, string description, CancellationToken ct = default);

    /// <summary>
    /// Ottiene lo storico transazioni di una carta
    /// </summary>
    Task<IEnumerable<FidelityPointsTransactionViewModel>> GetTransactionHistoryAsync(Guid cardId, CancellationToken ct = default);
}
