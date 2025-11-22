using System.Collections.Generic;

namespace EventForge.DTOs.Products
{
    /// <summary>
    /// Result of a bulk update operation.
    /// </summary>
    public class BulkUpdateResult
    {
        /// <summary>
        /// Total number of products requested for update.
        /// </summary>
        public int TotalRequested { get; set; }

        /// <summary>
        /// Number of products successfully updated.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of products that failed to update.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// List of errors that occurred during the update.
        /// </summary>
        public List<BulkUpdateError> Errors { get; set; } = new();

        /// <summary>
        /// Indicates if the transaction was rolled back due to errors.
        /// </summary>
        public bool RolledBack { get; set; }
    }
}
