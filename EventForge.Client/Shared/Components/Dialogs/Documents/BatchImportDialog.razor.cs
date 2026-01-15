using EventForge.Client.Services;
using EventForge.Client.Services.Documents;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace EventForge.Client.Shared.Components.Dialogs.Documents;

/// <summary>
/// Dialog for batch importing document rows from CSV files
/// </summary>
public partial class BatchImportDialog
{
    [Inject] private IDocumentHeaderService DocumentHeaderService { get; set; } = null!;
    [Inject] private ICsvImportService CsvImportService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private ITranslationService TranslationService { get; set; } = null!;
    [Inject] private ILogger<BatchImportDialog> Logger { get; set; } = null!;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    
    [Parameter] public Guid DocumentHeaderId { get; set; }

    private IBrowserFile? _selectedFile;
    private CsvImportResult? _importResult;
    private bool _isProcessing = false;

    /// <summary>
    /// Handles file selection
    /// </summary>
    private async Task OnFileSelected(IBrowserFile file)
    {
        if (file == null)
            return;

        _selectedFile = file;
        _isProcessing = true;
        _importResult = null;

        try
        {
            // Limit file size to 5MB
            const long maxFileSize = 5 * 1024 * 1024;
            const int maxFileSizeMB = 5;
            
            if (file.Size > maxFileSize)
            {
                Snackbar.Add(
                    TranslationService.GetTranslation("common.fileTooLarge", "File is too large. Maximum size is {0}MB", maxFileSizeMB),
                    Severity.Error);
                return;
            }

            using var stream = file.OpenReadStream(maxFileSize);
            
            var options = new CsvImportOptions
            {
                DocumentHeaderId = DocumentHeaderId,
                RequireProductCode = true
            };

            _importResult = await CsvImportService.ImportFromCsvAsync(stream, options);

            if (_importResult.ValidRows.Any())
            {
                Snackbar.Add(
                    TranslationService.GetTranslation("documents.csvParsedSuccess", 
                        "CSV parsed: {0} valid rows, {1} errors", 
                        _importResult.ValidRows.Count, 
                        _importResult.InvalidRows.Count),
                    _importResult.InvalidRows.Any() ? Severity.Warning : Severity.Success);
            }
            else
            {
                Snackbar.Add(
                    TranslationService.GetTranslation("documents.noValidRows", "No valid rows found in CSV"),
                    Severity.Warning);
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error parsing CSV file");
            Snackbar.Add(
                TranslationService.GetTranslation("documents.importError", "Error importing file: {0}", ex.Message),
                Severity.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// Imports the valid rows into the document
    /// </summary>
    private async Task ImportRows()
    {
        if (_importResult == null || !_importResult.ValidRows.Any())
            return;

        _isProcessing = true;

        try
        {
            int successCount = 0;
            int failureCount = 0;

            foreach (var row in _importResult.ValidRows)
            {
                try
                {
                    var result = await DocumentHeaderService.AddDocumentRowAsync(row);
                    if (result != null)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                        Logger.LogWarning("Failed to import row: {ProductCode}", row.ProductCode);
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Logger.LogError(ex, "Error importing row: {ProductCode}", row.ProductCode);
                }
            }

            if (failureCount == 0)
            {
                Snackbar.Add(
                    TranslationService.GetTranslation("documents.importSuccess", 
                        "{0} rows imported successfully", successCount),
                    Severity.Success);
                MudDialog.Close(DialogResult.Ok(successCount));
            }
            else
            {
                Snackbar.Add(
                    TranslationService.GetTranslation("documents.importPartialSuccess", 
                        "{0} rows imported, {1} failed", successCount, failureCount),
                    Severity.Warning);
                MudDialog.Close(DialogResult.Ok(successCount));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error importing rows");
            Snackbar.Add(
                TranslationService.GetTranslation("documents.importError", "Error importing rows"),
                Severity.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// Cancels the import
    /// </summary>
    private void Cancel()
    {
        MudDialog.Cancel();
    }
}
