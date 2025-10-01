using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for updating a ProductSupplier relationship.
    /// </summary>
    public class UpdateProductSupplierDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        [Required(ErrorMessage = "The product is required.")]
        public Guid ProductId { get; set; }

        /// <summary>
        /// Supplier identifier.
        /// </summary>
        [Required(ErrorMessage = "The supplier is required.")]
        public Guid SupplierId { get; set; }

        /// <summary>
        /// Supplier's product code/SKU.
        /// </summary>
        [MaxLength(100, ErrorMessage = "The supplier product code cannot exceed 100 characters.")]
        public string? SupplierProductCode { get; set; }

        /// <summary>
        /// Purchase description.
        /// </summary>
        [MaxLength(500, ErrorMessage = "The purchase description cannot exceed 500 characters.")]
        public string? PurchaseDescription { get; set; }

        /// <summary>
        /// Unit cost from this supplier.
        /// </summary>
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// Currency for the unit cost.
        /// </summary>
        [MaxLength(10, ErrorMessage = "The currency cannot exceed 10 characters.")]
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
        [MaxLength(1000, ErrorMessage = "The notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }
    }
}
