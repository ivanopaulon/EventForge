using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Services.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.Server.Pages.Dashboard;

/// <summary>
/// Server logs page — displays Serilog application logs stored in the EventLogger database.
/// Uses <see cref="ILogManagementService"/> (backed by Dapper + LogDb connection string) to
/// read the real log data instead of the mostly-empty SystemOperationLogs table.
/// </summary>
[Authorize(Roles = "SuperAdmin")]
public class LogsModel(ILogManagementService logManagementService, ILogger<LogsModel> logger) : PageModel
{
    public List<SystemLogDto> Logs { get; set; } = [];
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 50;

    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? LevelFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string? SourceFilter { get; set; }
    [BindProperty(SupportsGet = true)] public bool ErrorsOnly { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var queryParams = new ApplicationLogQueryParameters
            {
                Page = CurrentPage,
                PageSize = PageSize,
                SortBy = "Timestamp",
                SortDirection = "desc"
            };

            if (!string.IsNullOrWhiteSpace(SearchTerm))
                queryParams.Message = SearchTerm;

            if (!string.IsNullOrWhiteSpace(LevelFilter))
                queryParams.Level = LevelFilter;

            if (!string.IsNullOrWhiteSpace(SourceFilter))
                queryParams.Source = SourceFilter;

            if (ErrorsOnly)
                queryParams.HasException = true;

            var result = await logManagementService.GetApplicationLogsAsync(queryParams, HttpContext.RequestAborted);

            Logs = result.Items?.ToList() ?? [];
            TotalCount = result.TotalCount;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, Math.Max(1, TotalPages)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading application logs");
            ErrorMessage = "Impossibile caricare i log. Verificare la connessione al database EventLogger.";
        }
    }

    public string GetFilterQueryString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(SearchTerm)) parts.Add($"SearchTerm={Uri.EscapeDataString(SearchTerm)}");
        if (!string.IsNullOrEmpty(LevelFilter)) parts.Add($"LevelFilter={LevelFilter}");
        if (!string.IsNullOrEmpty(SourceFilter)) parts.Add($"SourceFilter={SourceFilter}");
        if (ErrorsOnly) parts.Add("ErrorsOnly=true");
        return parts.Any() ? "&" + string.Join("&", parts) : "";
    }
}
