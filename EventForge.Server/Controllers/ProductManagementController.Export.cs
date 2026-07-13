using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.PriceLists;
using Prym.DTOs.Products;
using Prym.DTOs.Promotions;
using Prym.DTOs.UnitOfMeasures;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class ProductManagementController
{

    /// <summary>
    /// Export all products to Excel or CSV (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="format">Export format: excel or csv (default: excel)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download (Excel or CSV)</returns>
    /// <response code="200">File ready for download</response>
    /// <response code="403">User not authorized for export operations</response>
    [HttpGet("products/export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {

        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 50000
        };

        var data = await productService.GetProductsForExportAsync(pagination, ct);

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (format.ToLowerInvariant())
        {
            case "csv":
                fileBytes = await exportService.ExportToCsvAsync(data, ct);
                contentType = "text/csv";
                fileName = $"Products_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                break;

            case "excel":
            default:
                fileBytes = await exportService.ExportToExcelAsync(data, "Products", ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Products_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                break;
        }

        logger.LogInformation(
            "Export completed: {FileName}, {Size} bytes, {Records} records",
            fileName, fileBytes.Length, data.Count());

        return File(fileBytes, contentType, fileName);
    }

}
