namespace Prym.DTOs.Station;

public enum StationOrderQueueStatus
{
    Waiting = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public class StationOrderQueueItemDto
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public Guid DocumentHeaderId { get; set; }
    public string? DocumentNumber { get; set; }
    public Guid? DocumentRowId { get; set; }
    public Guid? TeamMemberId { get; set; }
    public string? TeamMemberName { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public StationOrderQueueStatus Status { get; set; }
    public int SortOrder { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
