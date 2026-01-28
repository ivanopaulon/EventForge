using OfficeOpenXml;
using System.Globalization;
using System.Text;

namespace EventForge.Server.Services.Export;

/// <summary>
/// Implementation of IExportService using EPPlus for Excel and custom CSV generator
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
        
        // Set EPPlus license context (required for EPPlus 5+)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data, 
        string sheetName = "Data", 
        CancellationToken ct = default) where T : class
    {
        _logger.LogInformation("Starting Excel export for {Type}, SheetName: {SheetName}", 
            typeof(T).Name, sheetName);
        
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);
        
        // Load data using EPPlus reflection
        worksheet.Cells["A1"].LoadFromCollection(data, true);
        
        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        
        // Style header row
        using (var range = worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;
        }
        
        // Add filters
        worksheet.Cells[1, 1, worksheet.Dimension.Rows, worksheet.Dimension.Columns].AutoFilter = true;
        
        _logger.LogInformation("Excel export completed: {Rows} rows, {Columns} columns", 
            worksheet.Dimension.Rows - 1, worksheet.Dimension.Columns);
        
        return await Task.FromResult(package.GetAsByteArray());
    }

    public async Task<byte[]> ExportToCsvAsync<T>(
        IEnumerable<T> data, 
        CancellationToken ct = default) where T : class
    {
        _logger.LogInformation("Starting CSV export for {Type}", typeof(T).Name);
        
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        
        // Get properties via reflection
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead)
            .ToArray();
        
        // Write header
        var header = string.Join(",", properties.Select(p => EscapeCsvField(p.Name)));
        await writer.WriteLineAsync(header);
        
        // Write data rows
        foreach (var item in data)
        {
            ct.ThrowIfCancellationRequested();
            
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return value?.ToString() ?? string.Empty;
            });
            
            var line = string.Join(",", values.Select(EscapeCsvField));
            await writer.WriteLineAsync(line);
        }
        
        await writer.FlushAsync();
        
        _logger.LogInformation("CSV export completed: {Rows} rows", data.Count());
        
        return memoryStream.ToArray();
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "\"\"";
        
        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }
}
