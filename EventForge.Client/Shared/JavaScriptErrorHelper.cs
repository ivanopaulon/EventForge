using EventForge.Client.Services;
using Microsoft.JSInterop;
using MudBlazor;

namespace EventForge.Client.Shared
{
    /// <summary>
    /// Static helper for handling JavaScript errors that get called from JavaScript code.
    /// </summary>
    public static class JavaScriptErrorHelper
    {
        public static IServiceProvider? ServiceProvider { get; set; }

        [JSInvokable("HandleJavaScriptError")]
        public static async Task HandleJavaScriptError(JSErrorInfo errorInfo)
        {
            try
            {
                var serviceProvider = ServiceProvider;
                if (serviceProvider != null)
                {
                    // Get required services
                    var clientLogService = serviceProvider.GetService<IClientLogService>();
                    var snackbar = serviceProvider.GetService<ISnackbar>();

                    var message = $"JavaScript error: {errorInfo.Message}";
                    var properties = new Dictionary<string, object>
                    {
                        ["Source"] = errorInfo.Source,
                        ["Line"] = errorInfo.Line,
                        ["Column"] = errorInfo.Column,
                        ["Stack"] = errorInfo.Stack
                    };

                    // Log the error
                    if (clientLogService != null)
                    {
                        await clientLogService.LogErrorAsync(message, null, "JavaScriptErrorHandler", properties);
                    }

                    // Show user-friendly notification
                    if (snackbar != null)
                    {
                        _ = snackbar.Add(
                            "Si è verificato un errore nell'applicazione. L'errore è stato registrato.",
                            Severity.Error,
                            config =>
                            {
                                config.VisibleStateDuration = 4000;
                                config.ShowCloseIcon = true;
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to handle JavaScript error: {ex.Message}");
            }
        }
    }

    public class JSErrorInfo
    {
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public int Line { get; set; }
        public int Column { get; set; }
        public string Stack { get; set; } = string.Empty;
    }
}