using Prym.DTOs.Common;

namespace Prym.DTOs.Products
{
    /// <summary>
    /// Request DTO for bulk updating product catalog fields.
    /// Only non-null fields are applied during the update.
    /// Maximum 500 products per operation (explicit list or filter result).
    /// </summary>
    public class BulkUpdateProductsDto
    {
        /// <summary>
        /// Explicit list of product IDs to update. When provided, filter parameters are ignored.
        /// Maximum 500 IDs.
        /// </summary>
        public List<Guid>? ProductIds { get; set; }

        // ---- Optional pre-filter parameters (ignored when ProductIds is provided) ----

        /// <summary>
        /// Filter by brand ID.
        /// </summary>
        public Guid? FilterBrandId { get; set; }

        /// <summary>
        /// Filter by classification node ID (Category, Family, or Group). Includes all descendants.
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

        /// <summary>
        /// Filter by current product status.
        /// </summary>
        public ProductStatus? FilterStatus { get; set; }

        /// <summary>
        /// Filter by bundle flag. True = only bundles, False = only simple products, null = all.
        /// </summary>
        public bool? FilterIsBundle { get; set; }

        /// <summary>
        /// Filter by model ID.
        /// </summary>
        public Guid? FilterModelId { get; set; }

        /// <summary>
        /// Filter by station ID.
        /// </summary>
        public Guid? FilterStationId { get; set; }

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

        /// <summary>
        /// New product status to apply.
        /// </summary>
        public ProductStatus? Status { get; set; }

        /// <summary>
        /// New IsVatIncluded flag to apply. Null means no change.
        /// </summary>
        public bool? IsVatIncluded { get; set; }

        /// <summary>
        /// New reorder point to apply.
        /// </summary>
        public decimal? ReorderPoint { get; set; }

        /// <summary>
        /// New safety stock level to apply.
        /// </summary>
        public decimal? SafetyStock { get; set; }

        /// <summary>
        /// New target stock level to apply.
        /// </summary>
        public decimal? TargetStockLevel { get; set; }

        /// <summary>
        /// New average daily demand to apply.
        /// </summary>
        public decimal? AverageDailyDemand { get; set; }

        /// <summary>
        /// New preferred supplier ID to apply.
        /// </summary>
        public Guid? PreferredSupplierId { get; set; }

        /// <summary>
        /// New station ID to apply.
        /// </summary>
        public Guid? StationId { get; set; }

        /// <summary>
        /// Optional reason for the bulk update, recorded in the audit log.
        /// </summary>
        public string? Reason { get; set; }
    }
}
