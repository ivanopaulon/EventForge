using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for document export request parameters.
    /// </summary>
    public class DocumentExportRequestDto
    {
        /// <summary>
        /// Optional tenant filter for export.
        /// </summary>
        public Guid? TenantId { get; set; }

        /// <summary>
        /// Optional document type filter for export.
        /// </summary>
        public Guid? DocumentTypeId { get; set; }

        /// <summary>
        /// Optional document IDs to export (if not provided, exports all matching filters).
        /// </summary>
        public List<Guid>? DocumentIds { get; set; }

        /// <summary>
        /// Start date for export range.
        /// </summary>
        [Required]
        public DateTime FromDate { get; set; }

        /// <summary>
        /// End date for export range.
        /// </summary>
        [Required]
        public DateTime ToDate { get; set; }

        /// <summary>
        /// Export format (PDF, Excel, HTML, CSV, JSON).
        /// </summary>
        [Required]
        public string Format { get; set; } = "PDF";

        /// <summary>
        /// Whether to include document rows in export.
        /// </summary>
        public bool IncludeRows { get; set; } = true;

        /// <summary>
        /// Whether to include attachments in export.
        /// </summary>
        public bool IncludeAttachments { get; set; } = false;

        /// <summary>
        /// Whether to include comments in export.
        /// </summary>
        public bool IncludeComments { get; set; } = false;

        /// <summary>
        /// Optional search term to filter documents.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Maximum number of records to export (for performance).
        /// </summary>
        [Range(1, 10000)]
        public int? MaxRecords { get; set; }

        /// <summary>
        /// Document status filter.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Optional custom template ID for PDF/HTML exports.
        /// </summary>
        public Guid? TemplateId { get; set; }
    }

    /// <summary>
    /// DTO for document export operation result.
    /// </summary>
    public class DocumentExportResultDto
    {
        /// <summary>
        /// Export operation ID.
        /// </summary>
        public Guid ExportId { get; set; }

        /// <summary>
        /// Export status (Preparing, Processing, Completed, Failed).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Export format.
        /// </summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>
        /// Number of documents included in export.
        /// </summary>
        public int DocumentCount { get; set; }

        /// <summary>
        /// Download URL for completed export.
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// File name of the export.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// File size in bytes.
        /// </summary>
        public long? FileSizeBytes { get; set; }

        /// <summary>
        /// Export creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Estimated completion time for pending exports.
        /// </summary>
        public DateTime? EstimatedCompletionTime { get; set; }

        /// <summary>
        /// Export completion timestamp.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Error message if export failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Additional export metadata.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
