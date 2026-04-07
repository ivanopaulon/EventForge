using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Configuration;

/// <summary>
/// Stores JWT signing key history for secure key rotation.
/// Supports multiple active keys to allow smooth transition during rotation.
/// </summary>
public class JwtKeyHistory
{
    /// <summary>
    /// Unique identifier for the key history entry.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique identifier for this key (e.g., "key_2026-01-29_abc123").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string KeyIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The JWT signing key, encrypted using AES-256.
    /// </summary>
    [Required]
    public string EncryptedKey { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this key is currently active for signing new tokens.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date and time from which this key is valid for token validation.
    /// </summary>
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time until which this key is valid for token validation.
    /// Null means the key is valid indefinitely.
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Date and time when this key was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User or system that created this key.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
}
