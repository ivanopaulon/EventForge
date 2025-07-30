using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Products
{

    /// <summary>
    /// DTO for ProductBundleItem creation operations.
    /// </summary>
    public class CreateProductBundleItemDto
    {
        /// <summary>
        /// Bundle product identifier.
        /// </summary>
        [Required(ErrorMessage = "The bundle product is required.")]
        [Display(Name = "Bundle Product", Description = "Parent bundle product.")]
        public Guid BundleProductId { get; set; }

        /// <summary>
        /// Component product identifier.
        /// </summary>
        [Required(ErrorMessage = "The component product is required.")]
        [Display(Name = "Component Product", Description = "Component product (child).")]
        public Guid ComponentProductId { get; set; }

        /// <summary>
        /// Quantity of the component in the bundle.
        /// </summary>
        [Range(1, 10000, ErrorMessage = "Quantity must be between 1 and 10,000.")]
        [Display(Name = "Quantity", Description = "Quantity of the component in the bundle.")]
        public int Quantity { get; set; } = 1;
    }
}
