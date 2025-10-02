using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales
{

/// <summary>
/// DTO for payment method.
/// </summary>
public class PaymentMethodDto
{
    /// <summary>
    /// Payment method identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Payment method code.
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

    /// <summary>
    /// Requires integration flag.
    /// </summary>
    public bool RequiresIntegration { get; set; }

    /// <summary>
    /// Allows change flag.
    /// </summary>
    public bool AllowsChange { get; set; }
}

/// <summary>
/// DTO for creating a payment method.
/// </summary>
public class CreatePaymentMethodDto
{
    /// <summary>
    /// Payment method code.
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

    /// <summary>
    /// Requires integration flag.
    /// </summary>
    public bool RequiresIntegration { get; set; }

    /// <summary>
    /// Integration configuration (JSON).
    /// </summary>
    [MaxLength(2000)]
    public string? IntegrationConfig { get; set; }

    /// <summary>
    /// Allows change flag.
    /// </summary>
    public bool AllowsChange { get; set; } = true;
}

/// <summary>
/// DTO for updating a payment method.
/// </summary>
public class UpdatePaymentMethodDto
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

    /// <summary>
    /// Integration configuration (JSON).
    /// </summary>
    [MaxLength(2000)]
    public string? IntegrationConfig { get; set; }

    /// <summary>
    /// Allows change flag.
    /// </summary>
    public bool AllowsChange { get; set; }
}
}
