using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace EventForge.DTOs.Business
{

/// <summary>
/// DTO for BusinessPartyAccounting output/display operations.
/// </summary>
public class BusinessPartyAccountingDto
{
    /// <summary>
    /// Unique identifier for the business party accounting.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the related BusinessParty.
    /// </summary>
    public Guid BusinessPartyId { get; set; }

    /// <summary>
    /// Business party name (for display purposes).
    /// </summary>
    public string? BusinessPartyName { get; set; }

    /// <summary>
    /// IBAN for payments.
    /// </summary>
    public string? Iban { get; set; }

    /// <summary>
    /// Foreign key to the related bank.
    /// </summary>
    public Guid? BankId { get; set; }

    /// <summary>
    /// Bank name (for display purposes).
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// Foreign key to the payment term.
    /// </summary>
    public Guid? PaymentTermId { get; set; }

    /// <summary>
    /// Payment term name (for display purposes).
    /// </summary>
    public string? PaymentTermName { get; set; }

    /// <summary>
    /// Assigned credit limit.
    /// </summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Additional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Date and time when the business party accounting was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the business party accounting.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Date and time when the business party accounting was last modified (UTC).
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User who last modified the business party accounting.
    /// </summary>
    public string? ModifiedBy { get; set; }
}}
