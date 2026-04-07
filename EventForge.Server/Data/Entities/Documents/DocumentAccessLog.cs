using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Documents;

/// <summary>
/// Represents an access log entry for a document.
/// Tracks all access to documents for security audit and compliance.
/// </summary>
public class DocumentAccessLog
{
    /// <summary>
    /// Unique identifier for this access log entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Document that was accessed.
    /// </summary>
    [Display(Name = "Document ID", Description = "Document that was accessed.")]
    public Guid? DocumentHeaderId { get; set; }

    /// <summary>
    /// Navigation property for the document.
    /// </summary>
    public DocumentHeader? DocumentHeader { get; set; }

    /// <summary>
    /// User who accessed the document.
    /// </summary>
    [Required]
    [StringLength(256)]
    [Display(Name = "User ID", Description = "User who accessed the document.")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's display name at the time of access.
    /// </summary>
    [StringLength(256)]
    [Display(Name = "User Name", Description = "User's display name at the time of access.")]
    public string? UserName { get; set; }

    /// <summary>
    /// Type of access operation (View, Download, Edit, Delete, Export, Print).
    /// </summary>
    [Required]
    [StringLength(50)]
    [Display(Name = "Access Type", Description = "Type of access operation.")]
    public string AccessType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the access.
    /// </summary>
    [Required]
    [Display(Name = "Accessed At", Description = "Timestamp of the access.")]
    public DateTime AccessedAt { get; set; }

    /// <summary>
    /// IP address of the client.
    /// </summary>
    [StringLength(45)] // IPv6 max length
    [Display(Name = "IP Address", Description = "IP address of the client.")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string of the client.
    /// </summary>
    [StringLength(500)]
    [Display(Name = "User Agent", Description = "User agent string of the client.")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Result of the access attempt (Success, Denied, Failed).
    /// </summary>
    [Required]
    [StringLength(50)]
    [Display(Name = "Result", Description = "Result of the access attempt.")]
    public string Result { get; set; } = "Success";

    /// <summary>
    /// Additional details about the access.
    /// </summary>
    [StringLength(1000)]
    [Display(Name = "Details", Description = "Additional details about the access.")]
    public string? Details { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenant isolation.
    /// </summary>
    [Required]
    [Display(Name = "Tenant ID", Description = "Tenant ID for multi-tenant isolation.")]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Session ID if available.
    /// </summary>
    [StringLength(100)]
    [Display(Name = "Session ID", Description = "Session ID if available.")]
    public string? SessionId { get; set; }
}

/// <summary>
/// Enumeration of document access types for logging.
/// </summary>
public static class DocumentAccessType
{
    public const string View = "View";
    public const string Download = "Download";
    public const string Edit = "Edit";
    public const string Delete = "Delete";
    public const string Export = "Export";
    public const string Print = "Print";
    public const string Create = "Create";
    public const string Approve = "Approve";
    public const string Reject = "Reject";
}

/// <summary>
/// Enumeration of access results.
/// </summary>
public static class AccessResult
{
    public const string Success = "Success";
    public const string Denied = "Denied";
    public const string Failed = "Failed";
}
