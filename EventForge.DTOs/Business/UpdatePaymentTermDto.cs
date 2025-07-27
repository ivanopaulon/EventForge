using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Business
{

/// <summary>
/// DTO for PaymentTerm update operations.
/// </summary>
public class UpdatePaymentTermDto
{
    /// <summary>
    /// Name or short description of the payment term.
    /// </summary>
    [Required(ErrorMessage = "The name is required.")]
    [MaxLength(100, ErrorMessage = "The name cannot exceed 100 characters.")]
    [Display(Name = "Name", Description = "Name or short description of the payment term.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the payment term.
    /// </summary>
    [MaxLength(250, ErrorMessage = "The description cannot exceed 250 characters.")]
    [Display(Name = "Description", Description = "Detailed description of the payment term.")]
    public string? Description { get; set; }

    /// <summary>
    /// Number of days for payment due.
    /// </summary>
    [Range(0, 365, ErrorMessage = "Due days must be between 0 and 365.")]
    [Display(Name = "Due Days", Description = "Number of days for payment due.")]
    public int DueDays { get; set; } = 0;

    /// <summary>
    /// Preferred payment method.
    /// </summary>
    [Required]
    [Display(Name = "Payment Method", Description = "Preferred payment method.")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

    /// <summary>
    /// Status of the payment term.
    /// </summary>
    [Required]
    [Display(Name = "Status", Description = "Current status of the payment term.")]
    public PaymentTermStatus Status { get; set; } = PaymentTermStatus.Active;
}}
