using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace EventForge.Server.Services.Common;

public class ExcelExportService : IExcelExportService
{
    public async Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        ExcelExportOptions options,
        CancellationToken cancellationToken = default)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(options.SheetName);
        
        var dataList = data.ToList();
        var properties = typeof(T).GetProperties();
        var visibleColumns = options.Columns.Where(c => c.IsVisible).ToList();
        
        if (!visibleColumns.Any())
            throw new InvalidOperationException("No visible columns");
        
        // Header row
        for (int colIndex = 0; colIndex < visibleColumns.Count; colIndex++)
        {
            var column = visibleColumns[colIndex];
            var cell = worksheet.Cells[1, colIndex + 1];
            cell.Value = column.DisplayName;
            cell.Style.Font.Bold = options.Formatting.HeaderBold;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(options.Formatting.HeaderBackgroundColor));
            cell.Style.Font.Color.SetColor(ColorTranslator.FromHtml(options.Formatting.HeaderFontColor));
            if (options.Formatting.HeaderBorders)
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }
        
        // Data rows
        for (int rowIndex = 0; rowIndex < dataList.Count; rowIndex++)
        {
            var item = dataList[rowIndex];
            var excelRow = rowIndex + 2;
            
            for (int colIndex = 0; colIndex < visibleColumns.Count; colIndex++)
            {
                var column = visibleColumns[colIndex];
                var property = properties.FirstOrDefault(p => p.Name == column.PropertyName);
                
                if (property != null)
                {
                    var cell = worksheet.Cells[excelRow, colIndex + 1];
                    var value = property.GetValue(item);
                    cell.Value = value;
                    
                    if (!string.IsNullOrEmpty(column.NumberFormat))
                        cell.Style.Numberformat.Format = column.NumberFormat;
                    
                    if (options.Formatting.AlternateRowColors && excelRow % 2 == 0)
                    {
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(options.Formatting.AlternateRowColor));
                    }
                    
                    if (options.Formatting.DataBorders)
                        cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }
        }
        
        if (options.AutoFitColumns && worksheet.Dimension != null)
        {
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            for (int i = 1; i <= visibleColumns.Count; i++)
                if (worksheet.Column(i).Width < 10)
                    worksheet.Column(i).Width = 10;
        }
        
        if (options.FreezeHeader)
            worksheet.View.FreezePanes(2, 1);
        
        if (options.AddAutoFilter && dataList.Any())
            worksheet.Cells[1, 1, dataList.Count + 1, visibleColumns.Count].AutoFilter = true;
        
        return await package.GetAsByteArrayAsync(cancellationToken);
    }
}
