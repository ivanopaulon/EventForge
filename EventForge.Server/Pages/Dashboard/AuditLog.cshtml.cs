using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.Server.Services.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace EventForge.Server.Pages.Dashboard;

[Authorize(Roles = "SuperAdmin")]
public class AuditLogModel : PageModel
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogModel> _logger;

    public AuditLogModel(
        IAuditLogService auditLogService,
        ILogger<AuditLogModel> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    // Filters
    [BindProperty(SupportsGet = true)]
    public string? EntityFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? OperationFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UserFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    // Pagination
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    // Data
    public List<EntityChangeLogDto> AuditLogs { get; set; } = new();
    public List<string> AvailableUsers { get; set; } = new();
    public AuditStatistics Statistics { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Build search criteria
            var searchDto = new DTOs.Audit.AuditTrailSearchDto
            {
                EntityName = EntityFilter,
                OperationType = OperationFilter,
                ChangedBy = UserFilter,
                FromDate = FromDate,
                ToDate = ToDate,
                SearchTerm = SearchTerm,
                Page = CurrentPage,
                PageSize = PageSize
            };

            // Get paginated audit logs
            var result = await _auditLogService.SearchAuditTrailAsync(searchDto, HttpContext.RequestAborted);

            // Map to DTO list
            AuditLogs = result.Items.Select(item => new EntityChangeLogDto
            {
                Id = item.Id,
                EntityName = item.EntityName,
                EntityDisplayName = item.EntityDisplayName,
                EntityId = item.EntityId,
                PropertyName = item.PropertyName,
                OperationType = item.OperationType,
                OldValue = item.OldValue,
                NewValue = item.NewValue,
                ChangedBy = item.ChangedBy,
                ChangedAt = item.ChangedAt
            }).ToList();

            TotalCount = (int)result.TotalCount;
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)PageSize);

            // Load statistics
            var stats = await _auditLogService.GetAuditTrailStatisticsAsync(HttpContext.RequestAborted);
            Statistics = new AuditStatistics
            {
                TotalLogs = stats.TotalEntries,
                TodayLogs = stats.TodayEntries,
                ThisWeekLogs = stats.ThisWeekEntries,
                SuperAdminLogs = stats.SuperAdminEntries,
                DeleteLogs = stats.DeletedEntries
            };

            // Load distinct users for filter dropdown
            var allLogs = await _auditLogService.GetAuditLogsAsync(
                new PaginationParameters { Page = 1, PageSize = 1000 }, 
                HttpContext.RequestAborted);
            AvailableUsers = allLogs.Items
                .Select(l => l.ChangedBy)
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit logs");
            TempData["Error"] = "Failed to load audit logs. Please try again.";
            return Page();
        }
    }

    /// <summary>
    /// AJAX handler for loading detail panel
    /// </summary>
    public async Task<IActionResult> OnGetDetailAsync(Guid id)
    {
        try
        {
            var log = await _auditLogService.GetLogByIdAsync(id, HttpContext.RequestAborted);
            
            if (log == null)
            {
                return NotFound();
            }

            var dto = new EntityChangeLogDto
            {
                Id = log.Id,
                EntityName = log.EntityName,
                EntityDisplayName = log.EntityDisplayName,
                EntityId = log.EntityId,
                PropertyName = log.PropertyName,
                OperationType = log.OperationType,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                ChangedBy = log.ChangedBy,
                ChangedAt = log.ChangedAt
            };

            return Partial("_AuditLogDetailPartial", dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading audit log detail for ID {LogId}", id);
            return StatusCode(500, "Failed to load audit log detail");
        }
    }

    /// <summary>
    /// CSV export handler
    /// </summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        try
        {
            var searchDto = new DTOs.Audit.AuditTrailSearchDto
            {
                EntityName = EntityFilter,
                OperationType = OperationFilter,
                ChangedBy = UserFilter,
                FromDate = FromDate,
                ToDate = ToDate,
                SearchTerm = SearchTerm,
                Page = 1,
                PageSize = 10000
            };

            var result = await _auditLogService.SearchAuditTrailAsync(searchDto, HttpContext.RequestAborted);

            // Generate CSV
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,Entity,EntityID,Operation,ChangedBy,Property,OldValue,NewValue");

            foreach (var log in result.Items)
            {
                csv.AppendLine($"\"{log.ChangedAt:yyyy-MM-dd HH:mm:ss}\",\"{log.EntityName}\",\"{log.EntityId}\",\"{log.OperationType}\",\"{log.ChangedBy}\",\"{log.PropertyName}\",\"{log.OldValue}\",\"{log.NewValue}\"");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"audit_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, "Failed to export audit logs");
        }
    }

    public string GetFilterQueryString()
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(EntityFilter))
            queryParams.Add($"EntityFilter={EntityFilter}");
        
        if (!string.IsNullOrEmpty(OperationFilter))
            queryParams.Add($"OperationFilter={OperationFilter}");
        
        if (!string.IsNullOrEmpty(UserFilter))
            queryParams.Add($"UserFilter={UserFilter}");
        
        if (FromDate.HasValue)
            queryParams.Add($"FromDate={FromDate.Value:yyyy-MM-dd}");
        
        if (ToDate.HasValue)
            queryParams.Add($"ToDate={ToDate.Value:yyyy-MM-dd}");
        
        if (!string.IsNullOrEmpty(SearchTerm))
            queryParams.Add($"SearchTerm={SearchTerm}");

        return queryParams.Any() ? "&" + string.Join("&", queryParams) : "";
    }
}

public class AuditStatistics
{
    public int TotalLogs { get; set; }
    public int TodayLogs { get; set; }
    public int ThisWeekLogs { get; set; }
    public int SuperAdminLogs { get; set; }
    public int DeleteLogs { get; set; }
}
