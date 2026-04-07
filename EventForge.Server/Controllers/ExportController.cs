using EventForge.Server.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EventForge.Server.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ExportController(IExcelExportService excelExportService) : BaseApiController
{

    [HttpPost("excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportToExcel([FromBody] JsonElement request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!request.TryGetProperty("options", out var optionsElement) ||
                !request.TryGetProperty("data", out var dataElement))
                return CreateValidationProblemDetails("Request must contain 'options' and 'data' properties.");

            var options = JsonSerializer.Deserialize<ExcelExportOptions>(
                optionsElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (options is null) return CreateValidationProblemDetails("Invalid options");

            var dataList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                dataElement.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dataList is null || !dataList.Any()) return CreateValidationProblemDetails("No data");

            var bytes = await excelExportService.ExportToExcelAsync(dataList, options, cancellationToken);
            var fileName = $"{options.FileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Export failed.", ex);
        }
    }
}
