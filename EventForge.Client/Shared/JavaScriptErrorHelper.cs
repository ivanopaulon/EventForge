using EventForge.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

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
                    var clientLogService = serviceProvider.GetService<IClientLogService>();
                    if (clientLogService != null)
                    {
                        var message = $"JavaScript error: {errorInfo.Message}";
                        var properties = new Dictionary<string, object>
                        {
                            ["Source"] = errorInfo.Source,
                            ["Line"] = errorInfo.Line,
                            ["Column"] = errorInfo.Column,
                            ["Stack"] = errorInfo.Stack
                        };

                        await clientLogService.LogErrorAsync(message, null, "JavaScriptErrorHandler", properties);
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