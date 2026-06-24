using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Station;

public class CreateStationOrderQueueItemDto
{
    [Required]
    public Guid StationId { get; set; }

    [Required]
    public Guid DocumentHeaderId { get; set; }

    public Guid? DocumentRowId { get; set; }

    public Guid? TeamMemberId { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 10000)]
    public int Quantity { get; set; } = 1;

    public int SortOrder { get; set; } = 0;

    public string? Notes { get; set; }
}
