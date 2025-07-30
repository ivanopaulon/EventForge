using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Products
{
    
    /// <summary>
    /// DTO for ProductBundleItem output/display operations.
    /// </summary>
    public class ProductBundleItemDto
    {
        /// <summary>
        /// Unique identifier for the product bundle item.
        /// </summary>
        public Guid Id { get; set; }
    
        /// <summary>
        /// Bundle product identifier.
        /// </summary>
        public Guid BundleProductId { get; set; }
    
        /// <summary>
        /// Component product identifier.
        /// </summary>
        public Guid ComponentProductId { get; set; }
    
        /// <summary>
        /// Quantity of the component in the bundle.
        /// </summary>
        public int Quantity { get; set; }
    
        /// <summary>
        /// Date and time when the bundle item was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }
    
        /// <summary>
        /// User who created the bundle item.
        /// </summary>
        public string? CreatedBy { get; set; }
    
        /// <summary>
        /// Date and time when the bundle item was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }
    
        /// <summary>
        /// User who last modified the bundle item.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
