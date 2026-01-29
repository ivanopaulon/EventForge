using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class HealthModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HealthModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string OverallStatus { get; set; } = "Unknown";
    public List<HealthCheckResult> HealthChecks { get; set; } = new();

    public async Task OnGetAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var response = await client.GetAsync($"{baseUrl}/health");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var healthReport = JsonSerializer.Deserialize<HealthReport>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (healthReport != null)
                {
                    OverallStatus = healthReport.Status;
                    HealthChecks = healthReport.Entries.Select(e => new HealthCheckResult
                    {
                        Name = e.Key,
                        Status = e.Value.Status,
                        Description = e.Value.Description ?? string.Empty,
                        Duration = e.Value.Duration,
                        Data = e.Value.Data ?? new Dictionary<string, object>()
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            OverallStatus = "Unhealthy";
            HealthChecks.Add(new HealthCheckResult
            {
                Name = "Error",
                Status = "Unhealthy",
                Description = ex.Message,
                Duration = TimeSpan.Zero,
                Data = new Dictionary<string, object>()
            });
        }
    }

    public class HealthReport
    {
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, HealthEntry> Entries { get; set; } = new();
    }

    public class HealthEntry
    {
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public class HealthCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }
}
