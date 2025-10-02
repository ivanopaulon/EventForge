using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales
{

/// <summary>
/// DTO for adding a note to a sale session.
/// </summary>
public class AddSessionNoteDto
{
    /// <summary>
    /// Note flag identifier.
    /// </summary>
    [Required(ErrorMessage = "Note flag ID is required")]
    public Guid NoteFlagId { get; set; }

    /// <summary>
    /// Note text.
    /// </summary>
    [Required(ErrorMessage = "Text is required")]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// DTO for a session note.
/// </summary>
public class SessionNoteDto
{
    /// <summary>
    /// Note identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Note flag identifier.
    /// </summary>
    public Guid NoteFlagId { get; set; }

    /// <summary>
    /// Note flag name.
    /// </summary>
    public string? NoteFlagName { get; set; }

    /// <summary>
    /// Note flag color.
    /// </summary>
    public string? NoteFlagColor { get; set; }

    /// <summary>
    /// Note flag icon.
    /// </summary>
    public string? NoteFlagIcon { get; set; }

    /// <summary>
    /// Note text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Created by user identifier.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Created by user name.
    /// </summary>
    public string? CreatedByUserName { get; set; }

    /// <summary>
    /// Created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for note flag.
/// </summary>
public class NoteFlagDto
{
    /// <summary>
    /// Flag identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Flag code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Color (hex code).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Icon identifier.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Is active flag.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for creating a new note flag.
/// </summary>
public class CreateNoteFlagDto
{
    /// <summary>
    /// Flag code.
    /// </summary>
    [Required(ErrorMessage = "Code is required")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Color (hex code).
    /// </summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// Icon identifier.
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Is active flag.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for updating a note flag.
/// </summary>
public class UpdateNoteFlagDto
{
    /// <summary>
    /// Display name.
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Color (hex code).
    /// </summary>
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// Icon identifier.
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// Is active flag.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    public int DisplayOrder { get; set; }
}
}
