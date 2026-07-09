using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Promotions
{

    /// <summary>
    /// DTO per duplicare una promozione esistente.
    /// </summary>
    public record DuplicatePromotionDto
    {
        [Required, MaxLength(100)]
        public required string Name { get; init; }

        /// <summary>
        /// Codice coupon del nuovo duplicato. Lasciare null: i codici coupon devono essere univoci,
        /// non ha senso copiarlo automaticamente dall'originale.
        /// </summary>
        [MaxLength(50)]
        public string? CouponCode { get; init; }

        public bool CopyRules { get; init; } = true;

        public DateTime? NewStartDate { get; init; }
        public DateTime? NewEndDate { get; init; }
    }
}
