using EventForge.DTOs.Printing;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.Client.Services.UI
{
    /// <summary>
    /// Interface for client-side printing service operations
    /// </summary>
    public interface IPrintingService
    {
        /// <summary>
        /// Discovers available printers through QZ Tray
        /// </summary>
        Task<PrinterDiscoveryResponseDto?> DiscoverPrintersAsync(PrinterDiscoveryRequestDto request);

        /// <summary>
        /// Checks the status of a specific printer
        /// </summary>
        Task<PrinterStatusResponseDto?> CheckPrinterStatusAsync(PrinterStatusRequestDto request);

        /// <summary>
        /// Submits a print job
        /// </summary>
        Task<SubmitPrintJobResponseDto?> SubmitPrintJobAsync(SubmitPrintJobRequestDto request);

        /// <summary>
        /// Gets the status of a specific print job
        /// </summary>
        Task<PrintJobDto?> GetPrintJobStatusAsync(Guid jobId);

        /// <summary>
        /// Cancels a print job
        /// </summary>
        Task<bool> CancelPrintJobAsync(Guid jobId);

        /// <summary>
        /// Tests connection to QZ Tray
        /// </summary>
        Task<bool> TestQzConnectionAsync(string qzUrl);

        /// <summary>
        /// Gets QZ Tray version information
        /// </summary>
        Task<string?> GetQzVersionAsync(string qzUrl);
    }

    /// <summary>
    /// Client-side service for printing operations
    /// </summary>
    public class PrintingService : IPrintingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PrintingService> _logger;

        public PrintingService(IHttpClientFactory httpClientFactory, ILogger<PrintingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<PrinterDiscoveryResponseDto?> DiscoverPrintersAsync(PrinterDiscoveryRequestDto request)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.PostAsJsonAsync("api/printing/discover", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<PrinterDiscoveryResponseDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                _logger.LogWarning("Failed to discover printers. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering printers");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<PrinterStatusResponseDto?> CheckPrinterStatusAsync(PrinterStatusRequestDto request)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.PostAsJsonAsync("api/printing/status", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<PrinterStatusResponseDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                _logger.LogWarning("Failed to check printer status. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking printer status");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<SubmitPrintJobResponseDto?> SubmitPrintJobAsync(SubmitPrintJobRequestDto request)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.PostAsJsonAsync("api/printing/print", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<SubmitPrintJobResponseDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                _logger.LogWarning("Failed to submit print job. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting print job");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<PrintJobDto?> GetPrintJobStatusAsync(Guid jobId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.GetAsync($"api/printing/jobs/{jobId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<PrintJobDto>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                _logger.LogWarning("Failed to get print job status. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting print job status for job: {JobId}", jobId);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CancelPrintJobAsync(Guid jobId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.PostAsync($"api/printing/jobs/{jobId}/cancel", null);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<bool>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                _logger.LogWarning("Failed to cancel print job. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling print job: {JobId}", jobId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> TestQzConnectionAsync(string qzUrl)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.PostAsJsonAsync("api/printing/test-connection", qzUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<bool>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                _logger.LogWarning("Failed to test QZ connection. Status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing QZ connection for URL: {QzUrl}", qzUrl);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<string?> GetQzVersionAsync(string qzUrl)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ApiClient");
                var response = await httpClient.PostAsJsonAsync("api/printing/version", qzUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<string>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                _logger.LogWarning("Failed to get QZ version. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting QZ version for URL: {QzUrl}", qzUrl);
                return null;
            }
        }
    }
}