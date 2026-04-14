using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Prym.ManagementHub.Pages;

/// <summary>
/// Update history page model for the Hub web UI.
/// Displays update events across all installations with pagination (50/page).
/// Supports filtering by status and installation name.
/// </summary>
public class HistoryModel(ManagementHubDbContext db) : PageModel
{
    public const int PageSize = 50;

    public IReadOnlyList<HistoryRow> History { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int CurrentPage { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public string? FilterStatus { get; private set; }
    public string? FilterInstallation { get; private set; }

    public async Task OnGetAsync(int page = 1, string? status = null, string? installation = null)
    {
        CurrentPage = Math.Max(1, page);
        FilterStatus = status;
        FilterInstallation = installation;

        var query = db.UpdateHistories
            .Include(h => h.Installation)
            .Include(h => h.Package)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<UpdateHistoryStatus>(status, true, out var parsedStatus))
            query = query.Where(h => h.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(installation))
        {
            var lowerFilter = installation.ToLowerInvariant();
            query = query.Where(h => h.Installation != null &&
                                     h.Installation.Name.ToLower().Contains(lowerFilter));
        }

        TotalCount = await query.CountAsync();

        var rows = await query
            .OrderByDescending(h => h.StartedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        History = rows
            .Select(h => new HistoryRow(
                h,
                h.Installation?.Name ?? "—",
                h.Package?.Version ?? "—",
                h.Package?.Component))
            .ToList();
    }

    public async Task<IActionResult> OnPostPurgeAsync(int olderThanDays = 30)
    {
        if (olderThanDays < 1) olderThanDays = 1;
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);

        var deleted = await db.UpdateHistories
            .Where(h => h.Status != UpdateHistoryStatus.InProgress
                        && h.StartedAt < cutoff)
            .ExecuteDeleteAsync();

        TempData["Success"] = $"Eliminati {deleted} record di cronologia precedenti al {cutoff:dd/MM/yyyy}.";
        return RedirectToPage();
    }

    public record HistoryRow(
        UpdateHistory History,
        string InstallationName,
        string PackageVersion,
        PackageComponent? Component);
}
