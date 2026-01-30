namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for recent product transactions (purchases or sales).
    /// Used to suggest recent prices when adding document rows.
    /// </summary>
    public class RecentProductTransactionDto
    {
        /// <summary>
        /// Document header ID.
        /// </summary>
        public Guid DocumentHeaderId { get; set; }

        /// <summary>
        /// Document number.
        /// </summary>
        public string DocumentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Document date.
        /// </summary>
        public DateTime DocumentDate { get; set; }

        /// <summary>
        /// Document row ID.
        /// </summary>
        public Guid DocumentRowId { get; set; }

        /// <summary>
        /// Business party ID (supplier for purchases, customer for sales).
        /// </summary>
        public Guid PartyId { get; set; }

        /// <summary>
        /// Business party name (supplier or customer).
        /// </summary>
        public string PartyName { get; set; } = string.Empty;

        /// <summary>
        /// Product ID.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Quantity (normalized to base quantity if available).
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Effective unit price (after discount, normalized to base unit).
        /// </summary>
        public decimal EffectiveUnitPrice { get; set; }

        /// <summary>
        /// Raw unit price as stored in the document row.
        /// </summary>
        public decimal UnitPriceRaw { get; set; }

        /// <summary>
        /// Base unit price (if available).
        /// </summary>
        public decimal? BaseUnitPrice { get; set; }

        /// <summary>
        /// Currency code (default: EUR).
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Unit of measure name.
        /// </summary>
        public string? UnitOfMeasure { get; set; }

        /// <summary>
        /// Discount type applied.
        /// </summary>
        public string? DiscountType { get; set; }

        /// <summary>
        /// Discount percentage or value applied.
        /// </summary>
        public decimal Discount { get; set; }
    }
}
