namespace EventForge.Server.Services.Common;

public class ExcelExportOptions
{
    public string FileName { get; set; } = "Export";
    public string SheetName { get; set; } = "Data";
    public List<ExcelColumnDefinition> Columns { get; set; } = new();
    public ExcelFormattingOptions Formatting { get; set; } = new();
    public bool AutoFitColumns { get; set; } = true;
    public bool FreezeHeader { get; set; } = true;
    public bool AddAutoFilter { get; set; } = true;
}

public class ExcelColumnDefinition
{
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? NumberFormat { get; set; }
    public bool IsVisible { get; set; } = true;
}

public class ExcelFormattingOptions
{
    public string HeaderBackgroundColor { get; set; } = "#1976D2";
    public string HeaderFontColor { get; set; } = "#FFFFFF";
    public bool HeaderBold { get; set; } = true;
    public bool HeaderBorders { get; set; } = true;
    public bool AlternateRowColors { get; set; } = true;
    public string AlternateRowColor { get; set; } = "#F5F5F5";
    public bool DataBorders { get; set; } = true;
}
