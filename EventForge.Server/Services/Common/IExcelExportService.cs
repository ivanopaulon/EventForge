namespace EventForge.Server.Services.Common;

public interface IExcelExportService
{
    Task<byte[]> ExportToExcelAsync<T>(
        IEnumerable<T> data,
        ExcelExportOptions options,
        CancellationToken cancellationToken = default);
}
