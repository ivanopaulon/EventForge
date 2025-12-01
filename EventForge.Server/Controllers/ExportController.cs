using EventForge.Server.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventForge.Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IExcelExportService excelExportService, ILogger<ExportController> logger)
    {
        _excelExportService = excelExportService;
        _logger = logger;
    }

    [HttpPost("excel")]
    public async Task<IActionResult> ExportToExcel([FromBody] JsonElement request, CancellationToken cancellationToken)
    {
        try
        {
            var options = JsonSerializer.Deserialize<ExcelExportOptions>(
                request.GetProperty("options").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (options == null) return BadRequest("Invalid options");

            var dataList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                request.GetProperty("data").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dataList == null || !dataList.Any()) return BadRequest("No data");

            var bytes = await _excelExportService.ExportToExcelAsync(dataList, options, cancellationToken);
            var fileName = $"{options.FileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel export failed");
            return StatusCode(500, "Export failed");
        }
    }
}
