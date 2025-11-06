namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO that combines Product with the ProductCode context used for lookup.
    /// Used when retrieving a product by code/barcode to provide immediate context
    /// about which code was matched and its associated ProductUnitId.
    /// </summary>
    public class ProductWithCodeDto
    {
        /// <summary>
        /// The product information.
        /// </summary>
        public ProductDto Product { get; set; } = null!;

        /// <summary>
        /// The product code that was matched during lookup.
        /// Includes ProductUnitId if the barcode is associated with a specific unit of measure.
        /// Can be null if the product was not found or if there was an error during lookup.
        /// </summary>
        public ProductCodeDto? Code { get; set; }
    }
}
