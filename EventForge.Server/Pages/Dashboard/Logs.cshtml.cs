using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Pages.Dashboard;

/// <summary>
/// Server logs page - displays and allows searching through system operation logs.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class LogsModel : PageModel
{
    private readonly EventForgeDbContext _context;
    private readonly ILogger<LogsModel> _logger;

    public List<SystemOperationLog> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 50;

    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? SeverityFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? CategoryFilter { get; set; }
    [BindProperty(SupportsGet = true)] public bool ErrorsOnly { get; set; }

    public List<string> AvailableCategories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public LogsModel(EventForgeDbContext context, ILogger<LogsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        try
        {
            AvailableCategories = await _context.SystemOperationLogs
                .Where(l => l.Category != null)
                .Select(l => l.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(HttpContext.RequestAborted);

            var query = _context.SystemOperationLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.ToLower();
                query = query.Where(l =>
                    (l.Description != null && l.Description.ToLower().Contains(term)) ||
                    (l.Operation != null && l.Operation.ToLower().Contains(term)) ||
                    l.ExecutedBy.ToLower().Contains(term) ||
                    l.Action.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(SeverityFilter))
                query = query.Where(l => l.Severity == SeverityFilter);

            if (!string.IsNullOrWhiteSpace(CategoryFilter))
                query = query.Where(l => l.Category == CategoryFilter);

            if (ErrorsOnly)
                query = query.Where(l => !l.Success || l.Severity == "Error" || l.Severity == "Critical");

            TotalCount = await query.CountAsync(HttpContext.RequestAborted);
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, Math.Max(1, TotalPages)));

            Logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system operation logs");
            ErrorMessage = "Impossibile caricare i log. Verificare la connessione al database.";
        }
    }

    public string GetFilterQueryString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(SearchTerm))   parts.Add($"SearchTerm={Uri.EscapeDataString(SearchTerm)}");
        if (!string.IsNullOrEmpty(SeverityFilter)) parts.Add($"SeverityFilter={SeverityFilter}");
        if (!string.IsNullOrEmpty(CategoryFilter)) parts.Add($"CategoryFilter={CategoryFilter}");
        if (ErrorsOnly) parts.Add("ErrorsOnly=true");
        return parts.Any() ? "&" + string.Join("&", parts) : "";
    }
}
