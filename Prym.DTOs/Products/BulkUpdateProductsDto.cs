namespace Prym.DTOs.Products
{
    /// <summary>
    /// Request DTO for bulk updating product catalog fields.
    /// Only non-null fields are applied during the update.
    /// </summary>
    public class BulkUpdateProductsDto
    {
        /// <summary>
        /// Explicit list of product IDs to update. When provided, filter parameters are ignored.
        /// </summary>
        public List<Guid>? ProductIds { get; set; }

        // ---- Optional pre-filter parameters (ignored when ProductIds is provided) ----

        /// <summary>
        /// Filter by brand ID.
        /// </summary>
        public Guid? FilterBrandId { get; set; }

        /// <summary>
        /// Filter by classification node ID (Category, Family, or Group).
        /// </summary>
        public Guid? FilterClassificationNodeId { get; set; }

        /// <summary>
        /// Filter by current VAT rate ID.
        /// </summary>
        public Guid? FilterVatRateId { get; set; }

        /// <summary>
        /// Filter by current unit of measure ID.
        /// </summary>
        public Guid? FilterUnitOfMeasureId { get; set; }

        // ---- Fields to update (null = leave unchanged) ----

        /// <summary>
        /// New unit of measure ID to apply.
        /// </summary>
        public Guid? UnitOfMeasureId { get; set; }

        /// <summary>
        /// New VAT rate ID to apply.
        /// </summary>
        public Guid? VatRateId { get; set; }

        /// <summary>
        /// New brand ID to apply.
        /// </summary>
        public Guid? BrandId { get; set; }

        /// <summary>
        /// New model ID to apply.
        /// </summary>
        public Guid? ModelId { get; set; }

        /// <summary>
        /// New category node ID to apply.
        /// </summary>
        public Guid? CategoryNodeId { get; set; }

        /// <summary>
        /// New family node ID to apply.
        /// </summary>
        public Guid? FamilyNodeId { get; set; }

        /// <summary>
        /// New merchandise group node ID to apply.
        /// </summary>
        public Guid? GroupNodeId { get; set; }
    }
}
