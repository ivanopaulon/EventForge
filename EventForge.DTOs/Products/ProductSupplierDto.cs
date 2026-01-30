namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for ProductSupplier output operations.
    /// </summary>
    public class ProductSupplierDto
    {
        /// <summary>
        /// Unique identifier for the product-supplier relationship.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string? ProductName { get; set; }

        /// <summary>
        /// Supplier identifier.
        /// </summary>
        public Guid SupplierId { get; set; }

        /// <summary>
        /// Supplier name.
        /// </summary>
        public string? SupplierName { get; set; }

        /// <summary>
        /// Supplier's product code/SKU.
        /// </summary>
        public string? SupplierProductCode { get; set; }

        /// <summary>
        /// Purchase description.
        /// </summary>
        public string? PurchaseDescription { get; set; }

        /// <summary>
        /// Unit cost from this supplier.
        /// </summary>
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// Currency for the unit cost.
        /// </summary>
        public string? Currency { get; set; }

        /// <summary>
        /// Minimum order quantity.
        /// </summary>
        public int? MinOrderQty { get; set; }

        /// <summary>
        /// Order quantity increment.
        /// </summary>
        public int? IncrementQty { get; set; }

        /// <summary>
        /// Lead time in days for delivery.
        /// </summary>
        public int? LeadTimeDays { get; set; }

        /// <summary>
        /// Last purchase price.
        /// </summary>
        public decimal? LastPurchasePrice { get; set; }

        /// <summary>
        /// Date of last purchase.
        /// </summary>
        public DateTime? LastPurchaseDate { get; set; }

        /// <summary>
        /// Indicates if this is the preferred supplier for this product.
        /// </summary>
        public bool Preferred { get; set; }

        /// <summary>
        /// Additional notes.
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Date and time when the relationship was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the relationship.
        /// </summary>
        public string? CreatedBy { get; set; }
    }
}
