using System;
using System.Collections.Generic;
using EventForge.DTOs.Common;

namespace EventForge.DTOs.Business;

public class BusinessPartyGroupMemberDto
{
    public Guid Id { get; set; }
    public Guid BusinessPartyGroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? GroupColorHex { get; set; }
    public string? GroupIcon { get; set; }
    
    public Guid BusinessPartyId { get; set; }
    public string BusinessPartyName { get; set; } = string.Empty;
    public BusinessPartyType BusinessPartyType { get; set; }
    
    public DateTime? MemberSince { get; set; }
    public DateTime? MemberUntil { get; set; }
    public BusinessPartyGroupMemberStatus Status { get; set; }
    public int? OverridePriority { get; set; }
    public string? Notes { get; set; }
    public bool IsFeatured { get; set; }
    
    public bool IsCurrentlyValid =>
        Status == BusinessPartyGroupMemberStatus.Active &&
        (!MemberSince.HasValue || MemberSince.Value <= DateTime.UtcNow) &&
        (!MemberUntil.HasValue || MemberUntil.Value >= DateTime.UtcNow);
    
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class AddBusinessPartyToGroupDto
{
    public required Guid BusinessPartyId { get; init; }
    public DateTime? MemberSince { get; init; }
    public DateTime? MemberUntil { get; init; }
    public int? OverridePriority { get; init; }
    public string? Notes { get; init; }
    public bool IsFeatured { get; init; } = false;
}

public class BulkAddMembersDto
{
    public required Guid BusinessPartyGroupId { get; init; }
    public required List<Guid> BusinessPartyIds { get; init; }
    public DateTime? MemberSince { get; init; }
    public DateTime? MemberUntil { get; init; }
}

public class UpdateBusinessPartyGroupMemberDto
{
    public DateTime? MemberSince { get; init; }
    public DateTime? MemberUntil { get; init; }
    public BusinessPartyGroupMemberStatus Status { get; init; }
    public int? OverridePriority { get; init; }
    public string? Notes { get; init; }
    public bool IsFeatured { get; init; }
}

public class BulkOperationResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
