using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Documents
{
    /// <summary>
    /// DTO for document comment data transfer
    /// </summary>
    public class DocumentCommentDto
    {
        /// <summary>
        /// Unique identifier for the comment
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the document header (if comment is on header)
        /// </summary>
        public Guid? DocumentHeaderId { get; set; }

        /// <summary>
        /// Reference to the document row (if comment is on specific row)
        /// </summary>
        public Guid? DocumentRowId { get; set; }

        /// <summary>
        /// Comment content/text
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Comment type (Comment, Note, Question, Task, etc.)
        /// </summary>
        public string CommentType { get; set; } = string.Empty;

        /// <summary>
        /// Priority level of the comment
        /// </summary>
        public string Priority { get; set; } = string.Empty;

        /// <summary>
        /// Status of the comment (Open, Resolved, Closed)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Parent comment ID (for threaded comments/replies)
        /// </summary>
        public Guid? ParentCommentId { get; set; }

        /// <summary>
        /// Assigned user for task-type comments
        /// </summary>
        public string? AssignedTo { get; set; }

        /// <summary>
        /// Due date for task-type comments
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Date when the comment was resolved
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// User who resolved the comment
        /// </summary>
        public string? ResolvedBy { get; set; }

        /// <summary>
        /// Mentioned users in the comment
        /// </summary>
        public string? MentionedUsers { get; set; }

        /// <summary>
        /// Indicates if this comment is private/internal only
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Indicates if this comment is pinned/important
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Visibility level of the comment
        /// </summary>
        public string Visibility { get; set; } = string.Empty;

        /// <summary>
        /// Tags associated with the comment
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the comment
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Last modification timestamp
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the comment
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Collection of replies to this comment
        /// </summary>
        public List<DocumentCommentDto> Replies { get; set; } = new List<DocumentCommentDto>();
    }

    /// <summary>
    /// DTO for creating document comments
    /// </summary>
    public class CreateDocumentCommentDto
    {
        /// <summary>
        /// Reference to the document header (if comment is on header)
        /// </summary>
        public Guid? DocumentHeaderId { get; set; }

        /// <summary>
        /// Reference to the document row (if comment is on specific row)
        /// </summary>
        public Guid? DocumentRowId { get; set; }

        /// <summary>
        /// Comment content/text
        /// </summary>
        [Required(ErrorMessage = "Comment content is required.")]
        [StringLength(2000, ErrorMessage = "Comment content cannot exceed 2000 characters.")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Comment type
        /// </summary>
        public string CommentType { get; set; } = "Comment";

        /// <summary>
        /// Priority level of the comment
        /// </summary>
        public string Priority { get; set; } = "Normal";

        /// <summary>
        /// Parent comment ID (for threaded comments/replies)
        /// </summary>
        public Guid? ParentCommentId { get; set; }

        /// <summary>
        /// Assigned user for task-type comments
        /// </summary>
        [StringLength(100, ErrorMessage = "Assigned user cannot exceed 100 characters.")]
        public string? AssignedTo { get; set; }

        /// <summary>
        /// Due date for task-type comments
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Mentioned users in the comment
        /// </summary>
        [StringLength(500, ErrorMessage = "Mentioned users cannot exceed 500 characters.")]
        public string? MentionedUsers { get; set; }

        /// <summary>
        /// Indicates if this comment is private/internal only
        /// </summary>
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// Indicates if this comment is pinned/important
        /// </summary>
        public bool IsPinned { get; set; } = false;

        /// <summary>
        /// Visibility level of the comment
        /// </summary>
        public string Visibility { get; set; } = "Team";

        /// <summary>
        /// Tags associated with the comment
        /// </summary>
        [StringLength(200, ErrorMessage = "Tags cannot exceed 200 characters.")]
        public string? Tags { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        [StringLength(1000, ErrorMessage = "Metadata cannot exceed 1000 characters.")]
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// DTO for updating document comments
    /// </summary>
    public class UpdateDocumentCommentDto
    {
        /// <summary>
        /// Comment content/text
        /// </summary>
        [StringLength(2000, ErrorMessage = "Comment content cannot exceed 2000 characters.")]
        public string? Content { get; set; }

        /// <summary>
        /// Priority level of the comment
        /// </summary>
        public string? Priority { get; set; }

        /// <summary>
        /// Status of the comment
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Assigned user for task-type comments
        /// </summary>
        [StringLength(100, ErrorMessage = "Assigned user cannot exceed 100 characters.")]
        public string? AssignedTo { get; set; }

        /// <summary>
        /// Due date for task-type comments
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Indicates if this comment is pinned/important
        /// </summary>
        public bool? IsPinned { get; set; }

        /// <summary>
        /// Visibility level of the comment
        /// </summary>
        public string? Visibility { get; set; }

        /// <summary>
        /// Tags associated with the comment
        /// </summary>
        [StringLength(200, ErrorMessage = "Tags cannot exceed 200 characters.")]
        public string? Tags { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        [StringLength(1000, ErrorMessage = "Metadata cannot exceed 1000 characters.")]
        public string? Metadata { get; set; }
    }

    /// <summary>
    /// DTO for resolving comments
    /// </summary>
    public class ResolveCommentDto
    {
        /// <summary>
        /// Resolution notes
        /// </summary>
        [StringLength(500, ErrorMessage = "Resolution notes cannot exceed 500 characters.")]
        public string? ResolutionNotes { get; set; }
    }

    /// <summary>
    /// DTO for comment statistics
    /// </summary>
    public class DocumentCommentStatsDto
    {
        /// <summary>
        /// Total number of comments
        /// </summary>
        public int TotalComments { get; set; }

        /// <summary>
        /// Number of open comments
        /// </summary>
        public int OpenComments { get; set; }

        /// <summary>
        /// Number of resolved comments
        /// </summary>
        public int ResolvedComments { get; set; }

        /// <summary>
        /// Number of task-type comments
        /// </summary>
        public int TaskComments { get; set; }

        /// <summary>
        /// Number of high priority comments
        /// </summary>
        public int HighPriorityComments { get; set; }

        /// <summary>
        /// Number of comments assigned to current user
        /// </summary>
        public int AssignedToMe { get; set; }
    }
}