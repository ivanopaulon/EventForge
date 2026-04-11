using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.Agent.Pages;

/// <summary>
/// Maintenance window management page model for the Agent local web UI.
/// Allows operators to add or remove scheduled installation windows,
/// shows the currently active window, and lists pending updates.
/// </summary>
public class ScheduleModel(
    AgentOptions options,
    ILogger<ScheduleModel> logger) : PageModel
{
    private static readonly string AppSettingsPath =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public List<MaintenanceWindowOptions> Windows { get; private set; } = [];
    public int ActiveWindowIndex { get; private set; } = -1;

    public void OnGet()
    {
        Windows = [.. options.MaintenanceWindows];
        ActiveWindowIndex = FindActiveWindowIndex();
    }

    public IActionResult OnPostAddWindow(string[] days, string startTime, string endTime)
    {
        var parsed = days
            .Select(d => int.TryParse(d, out var n) ? (DayOfWeek?)((DayOfWeek)n) : null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        var window = new MaintenanceWindowOptions
        {
            DaysOfWeek = parsed,
            StartTime = startTime,
            EndTime = endTime
        };

        try
        {
            options.MaintenanceWindows.Add(window);
            PersistWindows();
            TempData["Success"] = "Maintenance window added.";
        }
        catch (Exception ex)
        {
            options.MaintenanceWindows.Remove(window);
            logger.LogError(ex, "Failed to save maintenance windows");
            TempData["Error"] = $"Failed to save: {ex.Message}";
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDeleteWindow(int index)
    {
        if (index < 0 || index >= options.MaintenanceWindows.Count)
        {
            TempData["Error"] = "Invalid window index.";
            return RedirectToPage();
        }

        try
        {
            options.MaintenanceWindows.RemoveAt(index);
            PersistWindows();
            TempData["Success"] = "Maintenance window removed.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove maintenance window");
            TempData["Error"] = $"Failed to save: {ex.Message}";
        }

        return RedirectToPage();
    }

    private void PersistWindows()
    {
        if (!System.IO.File.Exists(AppSettingsPath)) return;

        var json = System.IO.File.ReadAllText(AppSettingsPath);
        var root = JsonNode.Parse(json) as JsonObject ?? new JsonObject();

        var agentSection = (root[AgentOptions.SectionName] as JsonObject) ?? new JsonObject();

        var windowsArray = new JsonArray();
        foreach (var w in options.MaintenanceWindows)
        {
            var obj = new JsonObject
            {
                ["DaysOfWeek"] = new JsonArray(w.DaysOfWeek.Select(d => JsonValue.Create(d.ToString())).ToArray<JsonNode?>()),
                ["StartTime"] = w.StartTime,
                ["EndTime"] = w.EndTime
            };
            windowsArray.Add(obj);
        }

        agentSection["MaintenanceWindows"] = windowsArray;
        root[AgentOptions.SectionName] = agentSection;

        var options2 = new JsonSerializerOptions { WriteIndented = true };
        System.IO.File.WriteAllText(AppSettingsPath, root.ToJsonString(options2));
    }

    private int FindActiveWindowIndex()
    {
        var now = DateTime.Now;
        for (var i = 0; i < options.MaintenanceWindows.Count; i++)
        {
            var w = options.MaintenanceWindows[i];
            if (w.DaysOfWeek.Count > 0 && !w.DaysOfWeek.Contains(now.DayOfWeek)) continue;
            if (!TimeOnly.TryParse(w.StartTime, out var start) ||
                !TimeOnly.TryParse(w.EndTime, out var end)) continue;

            var current = TimeOnly.FromDateTime(now);
            if (start > end ? (current >= start || current <= end) : (current >= start && current <= end))
                return i;
        }
        return -1;
    }
}
