using EventForge.Server.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventForge.Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ExportController : BaseApiController
{
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IExcelExportService excelExportService, ILogger<ExportController> logger)
    {
        _excelExportService = excelExportService;
        _logger = logger;
    }

    [HttpPost("excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportToExcel([FromBody] JsonElement request, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = JsonSerializer.Deserialize<ExcelExportOptions>(
                request.GetProperty("options").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (options == null) return CreateValidationProblemDetails("Invalid options");

            var dataList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                request.GetProperty("data").GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dataList == null || !dataList.Any()) return CreateValidationProblemDetails("No data");

            var bytes = await _excelExportService.ExportToExcelAsync(dataList, options, cancellationToken);
            var fileName = $"{options.FileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel export failed");
            return CreateInternalServerErrorProblem("Export failed.", ex);
        }
    }
}
