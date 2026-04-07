using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Prym.Hub.Pages;

/// <summary>
/// Update history page model for the Hub web UI.
/// Displays the last 500 update events across all installations,
/// ordered by most recent first.
/// </summary>
public class HistoryModel(UpdateHubDbContext db) : PageModel
{
    public IReadOnlyList<HistoryRow> History { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var rows = await db.UpdateHistories
            .Include(h => h.Installation)
            .Include(h => h.Package)
            .OrderByDescending(h => h.StartedAt)
            .Take(500)
            .ToListAsync();

        History = rows
            .Select(h => new HistoryRow(
                h,
                h.Installation?.Name ?? "—",
                h.Package?.Version ?? "—",
                h.Package?.Component))
            .ToList();
    }

    public record HistoryRow(
        UpdateHistory History,
        string InstallationName,
        string PackageVersion,
        PackageComponent? Component);
}
