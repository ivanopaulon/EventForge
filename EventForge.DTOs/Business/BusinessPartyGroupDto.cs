using System;
using System.ComponentModel.DataAnnotations;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.Business;

public class BusinessPartyGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public BusinessPartyGroupType GroupType { get; set; }
    public string? ColorHex { get; set; }
    public string? Icon { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int ActiveMembersCount { get; set; }
    public int TotalMembersCount { get; set; }
    
    public bool IsCurrentlyValid => 
        IsActive && 
        (!ValidFrom.HasValue || ValidFrom.Value <= DateTime.UtcNow) &&
        (!ValidTo.HasValue || ValidTo.Value >= DateTime.UtcNow);
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

public class CreateBusinessPartyGroupDto
{
    public required string Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public BusinessPartyGroupType GroupType { get; init; } = BusinessPartyGroupType.Customer;
    
    [StringLength(7, MinimumLength = 7, ErrorMessage = "Il colore deve essere in formato #RRGGBB")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Formato colore non valido")]
    public string? ColorHex { get; init; } = "#1976D2";
    
    public string? Icon { get; init; } = "Group";
    public int Priority { get; init; } = 50;
    public bool IsActive { get; init; } = true;
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
}

public class UpdateBusinessPartyGroupDto
{
    public required string Name { get; init; }
    public string? Code { get; init; }
    public string? Description { get; init; }
    public BusinessPartyGroupType GroupType { get; init; }
    
    [StringLength(7, MinimumLength = 7, ErrorMessage = "Il colore deve essere in formato #RRGGBB")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Formato colore non valido")]
    public string? ColorHex { get; init; }
    
    public string? Icon { get; init; }
    public int Priority { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
}
