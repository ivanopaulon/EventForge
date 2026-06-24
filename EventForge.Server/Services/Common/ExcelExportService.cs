using ClosedXML.Excel;

namespace EventForge.Server.Services.Common;

public class ExcelExportService(ILogger<ExcelExportService> logger) : IExcelExportService
{
    public async Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        ExcelExportOptions options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add(SanitizeSheetName(options.SheetName));

                var dataList = data.ToList();
                var properties = typeof(T).GetProperties();
                var visibleColumns = options.Columns.Where(c => c.IsVisible).ToList();

                if (!visibleColumns.Any())
                    throw new InvalidOperationException("No visible columns");

                // Header row
                for (int colIndex = 0; colIndex < visibleColumns.Count; colIndex++)
                {
                    var column = visibleColumns[colIndex];
                    var cell = worksheet.Cell(1, colIndex + 1);
                    cell.Value = column.DisplayName;
                    cell.Style.Font.Bold = options.Formatting.HeaderBold;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml(options.Formatting.HeaderBackgroundColor);
                    cell.Style.Font.FontColor = XLColor.FromHtml(options.Formatting.HeaderFontColor);
                    if (options.Formatting.HeaderBorders)
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Data rows
                for (int rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
                {
                    var item = dataList[rowIndex];
                    var excelRow = rowIndex + 2;

                    for (int colIndex = 0; colIndex < visibleColumns.Count; colIndex++)
                    {
                        var column = visibleColumns[colIndex];

                        object? value;
                        if (item is IDictionary<string, object> dict)
                        {
                            dict.TryGetValue(column.PropertyName, out var rawValue);
                            value = UnwrapJsonElement(rawValue);
                        }
                        else
                        {
                            var property = properties.FirstOrDefault(p => p.Name == column.PropertyName);
                            value = property?.GetValue(item);
                        }

                        var cell = worksheet.Cell(excelRow, colIndex + 1);
                        if (value is not null)
                            cell.Value = XLCellValue.FromObject(value);
                        else
                            cell.Value = string.Empty;

                        if (!string.IsNullOrEmpty(column.NumberFormat))
                            cell.Style.NumberFormat.Format = column.NumberFormat;

                        if (options.Formatting.AlternateRowColors && excelRow % 2 == 0)
                        {
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(options.Formatting.AlternateRowColor);
                        }

                        if (options.Formatting.DataBorders)
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                }

                if (options.AutoFitColumns)
                {
                    worksheet.Columns().AdjustToContents();
                    for (int i = 1; i <= visibleColumns.Count; i++)
                        if (worksheet.Column(i).Width < 10)
                            worksheet.Column(i).Width = 10;
                }

                if (options.FreezeHeader)
                    worksheet.SheetView.FreezeRows(1);

                if (options.AddAutoFilter && dataList.Any())
                    worksheet.Range(1, 1, dataList.Count + 1, visibleColumns.Count).SetAutoFilter();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting data to Excel for sheet '{SheetName}'.", options.SheetName);
            throw;
        }
    }

    /// <summary>
    /// Removes characters that are invalid in an Excel worksheet name and truncates to 31 characters.
    /// Invalid characters: \ / ? * [ ] :
    /// Leading/trailing apostrophes are also stripped (Excel restriction).
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

    private static object? UnwrapJsonElement(object? raw) => raw switch
    {
        System.Text.Json.JsonElement el => el.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => el.GetString(),
            System.Text.Json.JsonValueKind.Number => el.TryGetDouble(out var d) ? d : (object?)el.ToString(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            _ => el.ToString()
        },
        _ => raw
    };
}
