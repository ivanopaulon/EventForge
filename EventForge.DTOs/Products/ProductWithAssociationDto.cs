using System;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for displaying products with their supplier association status.
    /// </summary>
    public class ProductWithAssociationDto
    {
        /// <summary>
        /// Product identifier.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product code.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Product name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Product description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Indicates if this product is associated with the supplier.
        /// </summary>
        public bool IsAssociated { get; set; }

        /// <summary>
        /// ProductSupplier ID if associated, null otherwise.
        /// </summary>
        public Guid? ProductSupplierId { get; set; }

        /// <summary>
        /// Unit cost from this supplier (if associated).
        /// </summary>
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// Supplier's product code (if associated).
        /// </summary>
        public string? SupplierProductCode { get; set; }

        /// <summary>
        /// Indicates if this is the preferred supplier for this product.
        /// </summary>
        public bool Preferred { get; set; }
    }
}
