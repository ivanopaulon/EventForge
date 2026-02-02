using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Sales;

/// <summary>
/// Request DTO for splitting a sale session.
/// </summary>
public class SplitSessionDto
{
    /// <summary>
    /// ID of the session to split.
    /// </summary>
    [Required]
    public Guid SessionId { get; set; }
    
    /// <summary>
    /// Number of people/parts to split into (2-20).
    /// </summary>
    [Required]
    [Range(2, 20, ErrorMessage = "Split deve essere tra 2 e 20 persone")]
    public int NumberOfPeople { get; set; }
    
    /// <summary>
    /// Type of split operation.
    /// </summary>
    [Required]
    public SplitTypeDto SplitType { get; set; }
    
    /// <summary>
    /// Item assignments (required only for SplitType = BY_ITEMS).
    /// Each item must be assigned to exactly one person.
    /// </summary>
    public List<SplitItemAssignmentDto>? ItemAssignments { get; set; }
    
    /// <summary>
    /// Custom percentages (required only for SplitType = PERCENTAGE).
    /// Must sum to exactly 100.
    /// </summary>
    public List<decimal>? Percentages { get; set; }
}

/// <summary>
/// Type of split operation.
/// </summary>
public enum SplitTypeDto
{
    /// <summary>
    /// Equal split among all people.
    /// </summary>
    Equal = 0,
    
    /// <summary>
    /// Manual assignment of items to people.
    /// </summary>
    ByItems = 1,
    
    /// <summary>
    /// Custom percentage split.
    /// </summary>
    Percentage = 2
}

/// <summary>
/// Assignment of a sale item to a person.
/// </summary>
public class SplitItemAssignmentDto
{
    /// <summary>
    /// ID of the sale item to assign.
    /// </summary>
    [Required]
    public Guid ItemId { get; set; }
    
    /// <summary>
    /// Zero-based index of the person (0 to NumberOfPeople-1).
    /// </summary>
    [Required]
    [Range(0, 19)]
    public int PersonIndex { get; set; }
}

/// <summary>
/// Result of a split operation.
/// </summary>
public class SplitResultDto
{
    /// <summary>
    /// Original session ID that was split.
    /// </summary>
    public Guid OriginalSessionId { get; set; }
    
    /// <summary>
    /// Child sessions created from the split.
    /// </summary>
    public List<SaleSessionDto> ChildSessions { get; set; } = new();
    
    /// <summary>
    /// Total amount that was split.
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Type of split that was performed.
    /// </summary>
    public SplitTypeDto SplitType { get; set; }
}
