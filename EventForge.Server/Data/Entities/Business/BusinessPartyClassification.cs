using System.ComponentModel.DataAnnotations;
using EventForge.Server.Data.Entities.Common;

namespace EventForge.Server.Data.Entities.Business;

/// <summary>
/// Join entity linking a BusinessParty to a ClassificationNode.
/// </summary>
public class BusinessPartyClassification : AuditableEntity
{
    [Required]
    public Guid BusinessPartyId { get; set; }
    public BusinessParty BusinessParty { get; set; } = null!;

    [Required]
    public Guid ClassificationNodeId { get; set; }
    public ClassificationNode ClassificationNode { get; set; } = null!;
}
