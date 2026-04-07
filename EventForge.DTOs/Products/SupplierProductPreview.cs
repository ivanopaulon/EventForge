namespace EventForge.DTOs.Products
{
    /// <summary>
    /// Preview of a supplier product update showing current and new values.
    /// </summary>
    public class SupplierProductPreview
    {
        /// <summary>
        /// Product ID.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// Current unit cost.
        /// </summary>
        public decimal? CurrentUnitCost { get; set; }

        /// <summary>
        /// New unit cost after the update.
        /// </summary>
        public decimal? NewUnitCost { get; set; }

        /// <summary>
        /// Delta (difference) between new and current unit cost.
        /// </summary>
        public decimal? Delta { get; set; }

        /// <summary>
        /// Current lead time in days.
        /// </summary>
        public int? CurrentLeadTimeDays { get; set; }

        /// <summary>
        /// New lead time in days.
        /// </summary>
        public int? NewLeadTimeDays { get; set; }

        /// <summary>
        /// Current currency.
        /// </summary>
        public string? CurrentCurrency { get; set; }

        /// <summary>
        /// New currency.
        /// </summary>
        public string? NewCurrency { get; set; }

        /// <summary>
        /// Current minimum order quantity.
        /// </summary>
        public int? CurrentMinOrderQty { get; set; }

        /// <summary>
        /// New minimum order quantity.
        /// </summary>
        public int? NewMinOrderQty { get; set; }

        /// <summary>
        /// Current preferred status.
        /// </summary>
        public bool CurrentPreferred { get; set; }

        /// <summary>
        /// New preferred status.
        /// </summary>
        public bool NewPreferred { get; set; }
    }
}
