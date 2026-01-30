using EventForge.Server.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class LogsModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private const int DefaultPageSize = 50;

    public LogsModel(EventForgeDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string? LogLevel { get; set; }

    [BindProperty(SupportsGet = true)]
    public int TimeRange { get; set; } = 24;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<LogEntryDto> Logs { get; set; } = new();

    public async Task OnGetAsync()
    {
        var query = _context.Set<LogEntry>().AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(LogLevel))
        {
            query = query.Where(l => l.Level == LogLevel);
        }

        var startTime = DateTime.UtcNow.AddHours(-TimeRange);
        query = query.Where(l => l.TimeStamp >= startTime);

        if (!string.IsNullOrEmpty(SearchTerm))
        {
            query = query.Where(l => l.Message.Contains(SearchTerm));
        }

        // Get total count
        TotalCount = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

        // Apply pagination
        Logs = await query
            .OrderByDescending(l => l.TimeStamp)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(l => new LogEntryDto
            {
                Id = l.Id,
                Timestamp = l.TimeStamp,
                Level = l.Level,
                Category = l.MachineName ?? "System",
                Message = l.Message,
                Exception = l.Exception
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostExportAsync(string? logLevel, int timeRange, string? search)
    {
        var query = _context.Set<LogEntry>().AsQueryable();

        if (!string.IsNullOrEmpty(logLevel))
        {
            query = query.Where(l => l.Level == logLevel);
        }

        var startTime = DateTime.UtcNow.AddHours(-timeRange);
        query = query.Where(l => l.TimeStamp >= startTime);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l => l.Message.Contains(search));
        }

        var logs = await query
            .OrderByDescending(l => l.TimeStamp)
            .Take(10000)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Level,Category,Message,Exception");

        foreach (var log in logs)
        {
            sb.AppendLine($"\"{log.TimeStamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Level}\",\"{log.MachineName}\",\"{log.Message.Replace("\"", "\"\"")}\",\"{log.Exception?.Replace("\"", "\"\"")}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"logs-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
    }

    public class LogEntryDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
    }
}
