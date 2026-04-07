using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents a comment or discussion item on a document (header or row)
/// </summary>
public class DocumentComment : AuditableEntity
{
    /// <summary>
    /// Reference to the document header (if comment is on header)
    /// </summary>
    [Display(Name = "Document Header", Description = "Reference to the document header (if comment is on header).")]
    public Guid? DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document header
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// Reference to the document row (if comment is on specific row)
    /// </summary>
    [Display(Name = "Document Row", Description = "Reference to the document row (if comment is on specific row).")]
    public Guid? DocumentRowId { get; set; }

    /// <summary>
    /// Navigation property for the document row
    /// </summary>
    public DocumentRow? DocumentRow { get; set; }

    /// <summary>
    /// Comment content/text
    /// </summary>
    [Required(ErrorMessage = "Comment content is required.")]
    [StringLength(2000, ErrorMessage = "Comment content cannot exceed 2000 characters.")]
    [Display(Name = "Content", Description = "Comment content/text.")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Comment type (Comment, Note, Question, Task, etc.)
    /// </summary>
    [Display(Name = "Comment Type", Description = "Comment type (Comment, Note, Question, Task, etc.).")]
    public DocumentCommentType CommentType { get; set; } = DocumentCommentType.Comment;

    /// <summary>
    /// Priority level of the comment
    /// </summary>
    [Display(Name = "Priority", Description = "Priority level of the comment.")]
    public CommentPriority Priority { get; set; } = CommentPriority.Normal;

    /// <summary>
    /// Status of the comment (Open, Resolved, Closed)
    /// </summary>
    [Display(Name = "Status", Description = "Status of the comment (Open, Resolved, Closed).")]
    public CommentStatus Status { get; set; } = CommentStatus.Open;

    /// <summary>
    /// Parent comment ID (for threaded comments/replies)
    /// </summary>
    [Display(Name = "Parent Comment", Description = "Parent comment ID (for threaded comments/replies).")]
    public Guid? ParentCommentId { get; set; }

    /// <summary>
    /// Navigation property for the parent comment
    /// </summary>
    public DocumentComment? ParentComment { get; set; }

    /// <summary>
    /// Collection of replies to this comment
    /// </summary>
    public ICollection<DocumentComment> Replies { get; set; } = new List<DocumentComment>();

    /// <summary>
    /// Assigned user for task-type comments
    /// </summary>
    [StringLength(100, ErrorMessage = "Assigned user cannot exceed 100 characters.")]
    [Display(Name = "Assigned To", Description = "Assigned user for task-type comments.")]
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Due date for task-type comments
    /// </summary>
    [Display(Name = "Due Date", Description = "Due date for task-type comments.")]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Date when the comment was resolved
    /// </summary>
    [Display(Name = "Resolved At", Description = "Date when the comment was resolved.")]
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// User who resolved the comment
    /// </summary>
    [StringLength(100, ErrorMessage = "Resolved by cannot exceed 100 characters.")]
    [Display(Name = "Resolved By", Description = "User who resolved the comment.")]
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Mentioned users in the comment (comma-separated usernames or @mentions)
    /// </summary>
    [StringLength(500, ErrorMessage = "Mentioned users cannot exceed 500 characters.")]
    [Display(Name = "Mentioned Users", Description = "Mentioned users in the comment (comma-separated usernames or @mentions).")]
    public string? MentionedUsers { get; set; }

    /// <summary>
    /// Indicates if this comment is private/internal only
    /// </summary>
    [Display(Name = "Is Private", Description = "Indicates if this comment is private/internal only.")]
    public bool IsPrivate { get; set; } = false;

    /// <summary>
    /// Indicates if this comment is pinned/important
    /// </summary>
    [Display(Name = "Is Pinned", Description = "Indicates if this comment is pinned/important.")]
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Visibility level of the comment
    /// </summary>
    [Display(Name = "Visibility", Description = "Visibility level of the comment.")]
    public CommentVisibility Visibility { get; set; } = CommentVisibility.Team;

    /// <summary>
    /// Tags associated with the comment
    /// </summary>
    [StringLength(200, ErrorMessage = "Tags cannot exceed 200 characters.")]
    [Display(Name = "Tags", Description = "Tags associated with the comment.")]
    public string? Tags { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    [StringLength(1000, ErrorMessage = "Metadata cannot exceed 1000 characters.")]
    [Display(Name = "Metadata", Description = "Additional metadata (JSON).")]
    public string? Metadata { get; set; }
}

/// <summary>
/// Document comment type enumeration
/// </summary>
public enum DocumentCommentType
{
    Comment,
    Note,
    Question,
    Task,
    Issue,
    Suggestion,
    Approval,
    Rejection
}

/// <summary>
/// Comment priority enumeration
/// </summary>
public enum CommentPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Comment status enumeration
/// </summary>
public enum CommentStatus
{
    Open,
    InProgress,
    Resolved,
    Closed,
    Cancelled
}

/// <summary>
/// Comment visibility enumeration
/// </summary>
public enum CommentVisibility
{
    Private,
    Team,
    Department,
    Organization,
    Public
}