using EventForge.Client.Models.Fidelity;

namespace EventForge.Client.Services.Mock;

/// <summary>
/// Servizio mock in-memory per la gestione delle carte fedeltà
/// Tutti i dati vengono resettati al refresh della pagina (comportamento atteso)
/// </summary>
public class MockFidelityService : IMockFidelityService
{
    private readonly List<FidelityCardViewModel> _cards = new();
    private readonly List<FidelityPointsTransactionViewModel> _transactions = new();
    private readonly Random _random = Random.Shared; // Thread-safe shared random
    
    public MockFidelityService()
    {
        InitializeSeedData();
    }
    
    private void InitializeSeedData()
    {
        // Card 1: Gold Attiva con punti
        var goldCard = new FidelityCardViewModel
        {
            Id = Guid.NewGuid(),
            CardNumber = GenerateCardNumber(),
            Type = FidelityCardType.Gold,
            Status = FidelityCardStatus.Active,
            IssuedDate = DateTime.Now.AddYears(-1),
            ValidFrom = DateTime.Now.AddYears(-1),
            ValidTo = DateTime.Now.AddMonths(11),
            CurrentPoints = 2500,
            TotalPointsEarned = 3500,
            TotalPointsRedeemed = 1000,
            DiscountPercentage = 10,
            HasPriorityAccess = true,
            HasBirthdayBonus = true,
            Notes = "Cliente VIP - Gold Card attiva"
        };
        _cards.Add(goldCard);
        
        // Transazioni per Gold Card
        _transactions.Add(new FidelityPointsTransactionViewModel
        {
            Id = Guid.NewGuid(),
            FidelityCardId = goldCard.Id,
            TransactionDate = DateTime.Now.AddMonths(-1),
            Type = TransactionType.Earned,
            Points = 500,
            BalanceAfter = 3000,
            Description = "Acquisto prodotti - Fattura #12345"
        });
        
        _transactions.Add(new FidelityPointsTransactionViewModel
        {
            Id = Guid.NewGuid(),
            FidelityCardId = goldCard.Id,
            TransactionDate = DateTime.Now.AddDays(-15),
            Type = TransactionType.Redeemed,
            Points = -500,
            BalanceAfter = 2500,
            Description = "Sconto applicato su acquisto - Fattura #12456"
        });
        
        // Card 2: Silver Scaduta
        var silverCard = new FidelityCardViewModel
        {
            Id = Guid.NewGuid(),
            CardNumber = GenerateCardNumber(),
            Type = FidelityCardType.Silver,
            Status = FidelityCardStatus.Expired,
            IssuedDate = DateTime.Now.AddYears(-2),
            ValidFrom = DateTime.Now.AddYears(-2),
            ValidTo = DateTime.Now.AddMonths(-2),
            CurrentPoints = 450,
            TotalPointsEarned = 1200,
            TotalPointsRedeemed = 750,
            DiscountPercentage = 5,
            HasPriorityAccess = false,
            HasBirthdayBonus = true,
            Notes = "Card scaduta - richiedere rinnovo"
        };
        _cards.Add(silverCard);
        
        // Transazioni per Silver Card
        _transactions.Add(new FidelityPointsTransactionViewModel
        {
            Id = Guid.NewGuid(),
            FidelityCardId = silverCard.Id,
            TransactionDate = DateTime.Now.AddMonths(-3),
            Type = TransactionType.Earned,
            Points = 200,
            BalanceAfter = 650,
            Description = "Bonus compleanno"
        });
        
        _transactions.Add(new FidelityPointsTransactionViewModel
        {
            Id = Guid.NewGuid(),
            FidelityCardId = silverCard.Id,
            TransactionDate = DateTime.Now.AddMonths(-3).AddDays(5),
            Type = TransactionType.Redeemed,
            Points = -200,
            BalanceAfter = 450,
            Description = "Riscatto punti - Buono sconto"
        });
    }
    
    private string GenerateCardNumber()
    {
        var parts = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            parts.Add(_random.Next(1000, 9999).ToString());
        }
        return string.Join("-", parts);
    }
    
    public Task<IEnumerable<FidelityCardViewModel>> GetAllCardsAsync()
    {
        return Task.FromResult(_cards.AsEnumerable());
    }
    
    public Task<FidelityCardViewModel?> GetCardByIdAsync(Guid id)
    {
        var card = _cards.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(card);
    }
    
    public Task<FidelityCardViewModel> CreateCardAsync(FidelityCardViewModel card)
    {
        card.Id = Guid.NewGuid();
        card.CardNumber = GenerateCardNumber();
        card.IssuedDate = DateTime.Now;
        _cards.Add(card);
        
        // Aggiungi transazione iniziale se ci sono punti
        if (card.CurrentPoints > 0)
        {
            _transactions.Add(new FidelityPointsTransactionViewModel
            {
                Id = Guid.NewGuid(),
                FidelityCardId = card.Id,
                TransactionDate = DateTime.Now,
                Type = TransactionType.Earned,
                Points = card.CurrentPoints,
                BalanceAfter = card.CurrentPoints,
                Description = "Punti iniziali all'emissione card"
            });
        }
        
        return Task.FromResult(card);
    }
    
    public Task<FidelityCardViewModel> UpdateCardAsync(FidelityCardViewModel card)
    {
        var index = _cards.FindIndex(c => c.Id == card.Id);
        if (index >= 0)
        {
            _cards[index] = card;
        }
        return Task.FromResult(card);
    }
    
    public Task RevokeCardAsync(Guid cardId)
    {
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            card.Status = FidelityCardStatus.Revoked;
        }
        return Task.CompletedTask;
    }
    
    public Task SuspendCardAsync(Guid cardId)
    {
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            card.Status = FidelityCardStatus.Suspended;
        }
        return Task.CompletedTask;
    }
    
    public Task ActivateCardAsync(Guid cardId)
    {
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            card.Status = FidelityCardStatus.Active;
        }
        return Task.CompletedTask;
    }
    
    public Task AddPointsAsync(Guid cardId, int points, string description)
    {
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card != null)
        {
            card.CurrentPoints += points;
            card.TotalPointsEarned += points;
            
            _transactions.Add(new FidelityPointsTransactionViewModel
            {
                Id = Guid.NewGuid(),
                FidelityCardId = cardId,
                TransactionDate = DateTime.Now,
                Type = TransactionType.Earned,
                Points = points,
                BalanceAfter = card.CurrentPoints,
                Description = description
            });
        }
        return Task.CompletedTask;
    }
    
    public Task RedeemPointsAsync(Guid cardId, int points, string description)
    {
        var card = _cards.FirstOrDefault(c => c.Id == cardId);
        if (card != null && card.CurrentPoints >= points)
        {
            card.CurrentPoints -= points;
            card.TotalPointsRedeemed += points;
            
            _transactions.Add(new FidelityPointsTransactionViewModel
            {
                Id = Guid.NewGuid(),
                FidelityCardId = cardId,
                TransactionDate = DateTime.Now,
                Type = TransactionType.Redeemed,
                Points = -points,
                BalanceAfter = card.CurrentPoints,
                Description = description
            });
        }
        return Task.CompletedTask;
    }
    
    public Task<IEnumerable<FidelityPointsTransactionViewModel>> GetTransactionHistoryAsync(Guid cardId)
    {
        var transactions = _transactions
            .Where(t => t.FidelityCardId == cardId)
            .OrderByDescending(t => t.TransactionDate)
            .AsEnumerable();
        
        return Task.FromResult(transactions);
    }
}
