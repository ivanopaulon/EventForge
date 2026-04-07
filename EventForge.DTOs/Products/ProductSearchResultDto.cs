namespace EventForge.DTOs.Products
{
    /// <summary>
    /// DTO for unified product search results.
    /// Supports both exact code match and text-based search.
    /// </summary>
    public class ProductSearchResultDto
    {
        /// <summary>
        /// True if the search term matches exactly a barcode/product code.
        /// </summary>
        public bool IsExactCodeMatch { get; set; }

        /// <summary>
        /// Product found with exact code match (when IsExactCodeMatch = true).
        /// Includes code context information.
        /// </summary>
        public ProductWithCodeDto? ExactMatch { get; set; }

        /// <summary>
        /// List of products found through text-based search.
        /// </summary>
        public List<ProductDto> SearchResults { get; set; } = new();

        /// <summary>
        /// Total count of search results.
        /// </summary>
        public int TotalCount { get; set; }
    }
}
