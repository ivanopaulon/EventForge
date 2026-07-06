using ClosedXML.Excel;
using System.Text;

namespace EventForge.Server.Services.Export;

/// <summary>
/// Implementation of IExportService using ClosedXML for Excel and custom CSV generator
/// </summary>
public class ExportService(ILogger<ExportService> logger) : IExportService
{
    public async Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        string sheetName = "Data",
        CancellationToken ct = default) where T : class
    {
        try
        {
            return await Task.Run(() =>
            {
                var dataList = data.ToList();
                var properties = typeof(T).GetProperties()
                    .Where(p => p.CanRead)
                    .ToArray();

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(SanitizeSheetName(sheetName));

                // Write header row
                for (int col = 0; col < properties.Length; col++)
                {
                    var cell = worksheet.Cell(1, col + 1);
                    cell.Value = properties[col].Name;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                }

                // Write data rows
                for (int row = 0; row < dataList.Count; row++)
                {
                    ct.ThrowIfCancellationRequested();
                    for (int col = 0; col < properties.Length; col++)
                    {
                        var value = properties[col].GetValue(dataList[row]);
                        var cell = worksheet.Cell(row + 2, col + 1);
                        if (value is not null)
                            cell.Value = XLCellValue.FromObject(value);
                        else
                            cell.Value = string.Empty;
                    }
                }

                // Add auto-filter and auto-fit columns
                if (properties.Length > 0 && dataList.Count > 0)
                    worksheet.Range(1, 1, dataList.Count + 1, properties.Length).SetAutoFilter();
                worksheet.Columns().AdjustToContents();

                logger.LogInformation("Excel export completed: {Rows} rows, {Columns} columns",
                    dataList.Count, properties.Length);

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }, ct);
        }
        catch
        {
            throw;
        }
    }

    public async Task<byte[]> ExportToCsvAsync<T>(
        IEnumerable<T> data,
        CancellationToken ct = default) where T : class
    {
        try
        {

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

            logger.LogInformation("CSV export completed: {Rows} rows", data.Count());

            return memoryStream.ToArray();
        }
        catch
        {
            throw;
        }
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

    /// <summary>
    /// Removes characters that are invalid in an Excel worksheet name and truncates to 31 characters.
    /// </summary>
    private static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Sheet1";

        var sanitized = new string(name
            .Where(c => c is not ('\\' or '/' or '?' or '*' or '[' or ']' or ':'))
            .ToArray())
            .Trim('\'');

        if (string.IsNullOrWhiteSpace(sanitized))
            return "Sheet1";

        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }

}
