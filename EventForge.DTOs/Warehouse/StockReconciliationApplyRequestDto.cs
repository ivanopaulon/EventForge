using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Request to apply stock reconciliation corrections
    /// </summary>
    public class StockReconciliationApplyRequestDto
    {
        /// <summary>
        /// List of Stock IDs to update with calculated quantities
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one item must be selected")]
        public List<Guid> ItemsToApply { get; set; } = new();

        /// <summary>
        /// Reason for the reconciliation
        /// </summary>
        [Required]
        [StringLength(500, ErrorMessage = "Reason must be less than 500 characters")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Whether to create adjustment movements for the changes (default: true)
        /// </summary>
        public bool CreateAdjustmentMovements { get; set; } = true;
    }
}
