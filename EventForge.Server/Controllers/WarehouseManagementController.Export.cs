using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class WarehouseManagementController
{

    /// <summary>
    /// Export all warehouses to Excel or CSV (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="format">Export format: excel or csv (default: excel)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download (Excel or CSV)</returns>
    /// <response code="200">File ready for download</response>
    /// <response code="403">User not authorized for export operations</response>
    [HttpGet("facilities/export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportWarehouses(
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {

        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 50000
        };

        var data = await warehouseFacade.GetWarehousesForExportAsync(pagination, ct);

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (format.ToLowerInvariant())
        {
            case "csv":
                fileBytes = await warehouseFacade.ExportToCsvAsync(data, ct);
                contentType = "text/csv";
                fileName = $"Warehouses_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                break;

            case "excel":
            default:
                fileBytes = await warehouseFacade.ExportToExcelAsync(data, "Warehouses", ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Warehouses_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                break;
        }

        logger.LogInformation(
            "Export completed: {FileName}, {Size} bytes, {Records} records",
            fileName, fileBytes.Length, data.Count());

        return File(fileBytes, contentType, fileName);
    }

    /// <summary>
    /// Export all inventory to Excel or CSV (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="format">Export format: excel or csv (default: excel)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download (Excel or CSV)</returns>
    /// <response code="200">File ready for download</response>
    /// <response code="403">User not authorized for export operations</response>
    [HttpGet("inventory/export")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportInventory(
        [FromQuery] string format = "excel",
        CancellationToken ct = default)
    {

        var pagination = new PaginationParameters
        {
            Page = 1,
            PageSize = 50000
        };

        var data = await warehouseFacade.GetInventoryForExportAsync(pagination, ct);

        byte[] fileBytes;
        string contentType;
        string fileName;

        switch (format.ToLowerInvariant())
        {
            case "csv":
                fileBytes = await warehouseFacade.ExportToCsvAsync(data, ct);
                contentType = "text/csv";
                fileName = $"Inventory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                break;

            case "excel":
            default:
                fileBytes = await warehouseFacade.ExportToExcelAsync(data, "Inventory", ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Inventory_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                break;
        }

        logger.LogInformation(
            "Export completed: {FileName}, {Size} bytes, {Records} records",
            fileName, fileBytes.Length, data.Count());

        return File(fileBytes, contentType, fileName);
    }

}
