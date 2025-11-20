using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;
using System.Net.Http.Json;
using System.Text;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Client-side service for log management operations.
    /// </summary>
    public class LogManagementService : ILogManagementService
    {
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<LogManagementService> _logger;

        public LogManagementService(
            IHttpClientService httpClientService,
            ILogger<LogManagementService> logger)
        {
            _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<PagedResult<SystemLogDto>> GetApplicationLogsAsync(
            ApplicationLogQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var queryString = BuildQueryString(queryParameters);
                var result = await _httpClientService.GetAsync<PagedResult<SystemLogDto>>(
                    $"api/v1/LogManagement/logs?{queryString}");

                return result ?? new PagedResult<SystemLogDto>
                {
                    Items = new List<SystemLogDto>(),
                    Page = queryParameters.Page,
                    PageSize = queryParameters.PageSize,
                    TotalCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application logs with parameters: {@QueryParameters}", queryParameters);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<SystemLogDto?> GetApplicationLogByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _httpClientService.GetAsync<SystemLogDto>($"api/v1/LogManagement/logs/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application log with ID: {LogId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetAvailableLogLevelsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _httpClientService.GetAsync<IEnumerable<string>>("api/v1/LogManagement/levels");
                return result ?? new List<string> { "Debug", "Information", "Warning", "Error", "Critical" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available log levels");
                throw;
            }
        }

        /// <summary>
        /// Builds a query string from the ApplicationLogQueryParameters object.
        /// </summary>
        private static string BuildQueryString(ApplicationLogQueryParameters parameters)
        {
            var queryItems = new List<string>();

            if (!string.IsNullOrEmpty(parameters.Level))
                queryItems.Add($"Level={Uri.EscapeDataString(parameters.Level)}");

            if (!string.IsNullOrEmpty(parameters.Source))
                queryItems.Add($"Source={Uri.EscapeDataString(parameters.Source)}");

            if (!string.IsNullOrEmpty(parameters.Message))
                queryItems.Add($"Message={Uri.EscapeDataString(parameters.Message)}");

            if (parameters.UserId.HasValue)
                queryItems.Add($"UserId={parameters.UserId.Value}");

            if (parameters.TenantId.HasValue)
                queryItems.Add($"TenantId={parameters.TenantId.Value}");

            if (parameters.FromDate.HasValue)
                queryItems.Add($"FromDate={parameters.FromDate.Value:yyyy-MM-ddTHH:mm:ss}");

            if (parameters.ToDate.HasValue)
                queryItems.Add($"ToDate={parameters.ToDate.Value:yyyy-MM-ddTHH:mm:ss}");

            if (parameters.HasException.HasValue)
                queryItems.Add($"HasException={parameters.HasException.Value.ToString().ToLower()}");

            queryItems.Add($"Page={parameters.Page}");
            queryItems.Add($"PageSize={parameters.PageSize}");

            if (!string.IsNullOrEmpty(parameters.SortBy))
                queryItems.Add($"SortBy={Uri.EscapeDataString(parameters.SortBy)}");

            if (!string.IsNullOrEmpty(parameters.SortDirection))
                queryItems.Add($"SortDirection={Uri.EscapeDataString(parameters.SortDirection)}");

            return string.Join("&", queryItems);
        }
    }
}
