using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Warehouse
{
    /// <summary>
    /// Request payload for batched stock reconciliation calculation.
    /// </summary>
    public class StockReconciliationBatchRequestDto
    {
        /// <summary>
        /// Specific stock ids to reconcile in the current batch.
        /// </summary>
        [Required]
        [MinLength(1)]
        public List<Guid> StockIds { get; set; } = new();

        /// <summary>
        /// Shared reconciliation filters applied to the selected stock ids.
        /// </summary>
        [Required]
        public StockReconciliationRequestDto Filters { get; set; } = new();
    }
}
