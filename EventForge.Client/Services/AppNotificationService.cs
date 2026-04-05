using EventForge.Client.Shared.Components.Dialogs;
using MudBlazor;

namespace EventForge.Client.Services;

/// <summary>
/// Centralized notification service that wraps ISnackbar with enriched error
/// details support via an EFDialog-based error detail dialog.
/// </summary>
public class AppNotificationService(
    ISnackbar snackbar,
    IDialogService dialogService) : IAppNotificationService
{

    /// <inheritdoc />
    public void ShowSuccess(string message)
    {
        snackbar.Add(message, Severity.Success);
    }

    /// <inheritdoc />
    public void ShowInfo(string message)
    {
        snackbar.Add(message, Severity.Info);
    }

    /// <inheritdoc />
    public void ShowWarning(string message, string? details = null, string? correlationId = null)
    {
        if (string.IsNullOrEmpty(details) && string.IsNullOrEmpty(correlationId))
        {
            snackbar.Add(message, Severity.Warning);
            return;
        }

        var errorInfo = new AppErrorInfo
        {
            Message = message,
            Details = details,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        snackbar.Add(message, Severity.Warning, config =>
        {
            config.VisibleStateDuration = 7000;
            config.ShowCloseIcon = true;
            config.Action = "Dettagli";
            config.ActionColor = Color.Warning;
            config.OnClick = _ => OpenErrorDialogAsync(errorInfo);
        });
    }

    /// <inheritdoc />
    public void ShowError(string message, string? details = null, Exception? exception = null, string? correlationId = null)
    {
        var hasExtraInfo = !string.IsNullOrEmpty(details) || exception != null || !string.IsNullOrEmpty(correlationId);

        if (!hasExtraInfo)
        {
            snackbar.Add(message, Severity.Error, config =>
            {
                config.VisibleStateDuration = 6000;
                config.ShowCloseIcon = true;
            });
            return;
        }

        var errorInfo = new AppErrorInfo
        {
            Message = message,
            Details = details,
            ExceptionMessage = exception?.Message,
            ExceptionType = exception?.GetType().Name,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        snackbar.Add(message, Severity.Error, config =>
        {
            config.VisibleStateDuration = 8000;
            config.ShowCloseIcon = true;
            config.Action = "Dettagli";
            config.ActionColor = Color.Error;
            config.OnClick = _ => OpenErrorDialogAsync(errorInfo);
        });
    }

    /// <inheritdoc />
    public void ShowHttpError(string message, HttpRequestException? ex = null)
    {
        string? correlationId = null;
        string? details = null;

        if (ex?.Data[AppErrorInfo.ProblemDetailsDataKey] is EventForge.DTOs.Common.ProblemDetailsDto problemDetails)
        {
            details = problemDetails.Detail;
            if (problemDetails.Extensions != null &&
                problemDetails.Extensions.TryGetValue("correlationId", out var corrId))
            {
                correlationId = corrId?.ToString();
            }
        }

        ShowError(message, details: details, exception: ex, correlationId: correlationId);
    }

    private async Task OpenErrorDialogAsync(AppErrorInfo errorInfo)
    {
        var parameters = new DialogParameters<ErrorDetailDialog>
        {
            { x => x.ErrorInfo, errorInfo }
        };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };
        await dialogService.ShowAsync<ErrorDetailDialog>("Dettagli errore", parameters, options);
    }
}
