namespace EventForge.DTOs.Audit;

public class AuditTrailStatisticsDto
{
    public int TotalEntries { get; set; }
    public int TodayEntries { get; set; }
    public int ThisWeekEntries { get; set; }
    public int SuperAdminEntries { get; set; }
    public int DeletedEntries { get; set; }
}
