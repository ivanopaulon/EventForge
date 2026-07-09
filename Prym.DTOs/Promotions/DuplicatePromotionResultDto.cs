namespace Prym.DTOs.Promotions
{

    /// <summary>
    /// Risultato dell'operazione di duplicazione di una promozione.
    /// </summary>
    public record DuplicatePromotionResultDto
    {
        public required PromotionDto NewPromotion { get; init; }
        public int RulesCopied { get; init; }
    }
}
