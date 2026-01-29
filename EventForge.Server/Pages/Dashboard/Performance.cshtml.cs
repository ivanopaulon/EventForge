using EventForge.Server.Services.Dashboard;
using EventForge.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class PerformanceModel : PageModel
{
    private readonly IPerformanceMetricsService _metricsService;

    public PerformanceModel(IPerformanceMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    public List<SlowQueryInfo> SlowQueries { get; set; } = new();

    public async Task OnGetAsync()
    {
        var metrics = await _metricsService.GetPerformanceMetricsAsync();
        SlowQueries = metrics.SlowQueries.Take(10).Select(m => new SlowQueryInfo
        {
            QueryText = TruncateQuery(m.QueryPreview, 100),
            AverageDurationMs = (int)m.AvgDurationMs,
            MaxDurationMs = (int)m.AvgDurationMs,
            ExecutionCount = m.ExecutionCount,
            LastExecuted = m.LastSeen
        }).ToList();
    }

    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var metrics = await _metricsService.GetPerformanceMetricsAsync();
        
        var csv = new StringBuilder();
        csv.AppendLine("Query,Avg Duration (ms),Count,Last Executed");
        
        foreach (var metric in metrics.SlowQueries.Take(100))
        {
            csv.AppendLine($"\"{metric.QueryPreview.Replace("\"", "\"\"")}\",{metric.AvgDurationMs},{metric.ExecutionCount},{metric.LastSeen:yyyy-MM-dd HH:mm:ss}");
        }
        
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"slow-queries-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
    }

    private static string TruncateQuery(string query, int maxLength)
    {
        if (string.IsNullOrEmpty(query) || query.Length <= maxLength)
            return query;
        
        return query.Substring(0, maxLength) + "...";
    }

    public class SlowQueryInfo
    {
        public string QueryText { get; set; } = string.Empty;
        public int AverageDurationMs { get; set; }
        public int MaxDurationMs { get; set; }
        public int ExecutionCount { get; set; }
        public DateTime LastExecuted { get; set; }
    }
}
