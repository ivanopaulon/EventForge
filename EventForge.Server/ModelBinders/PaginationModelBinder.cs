using EventForge.Server.Configuration;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace EventForge.Server.ModelBinders;

public class PaginationModelBinder : IModelBinder
{
    private readonly PaginationSettings _settings;
    private readonly ILogger<PaginationModelBinder> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PaginationModelBinder(
        IOptions<PaginationSettings> settings,
        ILogger<PaginationModelBinder> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        // Parse page and pageSize from query string
        var pageValue = bindingContext.ValueProvider.GetValue("page").FirstValue;
        var pageSizeValue = bindingContext.ValueProvider.GetValue("pageSize").FirstValue;

        var page = ParseInt(pageValue, 1, 1);  // Page defaults to 1
        var requestedPageSize = ParseInt(pageSizeValue, _settings.DefaultPageSize, 1);

        // Determine max page size based on context
        var maxPageSize = DetermineMaxPageSize(httpContext);

        // Apply capping if needed
        var wasCapped = false;
        var actualPageSize = requestedPageSize;

        if (requestedPageSize > maxPageSize)
        {
            actualPageSize = maxPageSize;
            wasCapped = true;

            var userName = httpContext.User?.Identity?.Name ?? "Anonymous";
            var path = httpContext.Request.Path.Value ?? "";

            _logger.LogWarning(
                "PageSize {RequestedPageSize} exceeds limit {MaxPageSize} for user '{UserName}' on path '{Path}'. Capping to maximum.",
                requestedPageSize, maxPageSize, userName, path);
        }
        else if (requestedPageSize > _settings.RecommendedPageSize)
        {
            var userName = httpContext.User?.Identity?.Name ?? "Anonymous";
            var path = httpContext.Request.Path.Value ?? "";

            _logger.LogInformation(
                "PageSize {RequestedPageSize} exceeds recommended size {RecommendedPageSize} for user '{UserName}' on path '{Path}'.",
                requestedPageSize, _settings.RecommendedPageSize, userName, path);
        }

        var result = new PaginationParameters(page, actualPageSize)
        {
            WasCapped = wasCapped,
            AppliedMaxPageSize = maxPageSize
        };

        bindingContext.Result = ModelBindingResult.Success(result);
        return Task.CompletedTask;
    }

    private int DetermineMaxPageSize(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value ?? "";
        var user = httpContext.User;

        // Priority 1: Endpoint override (exact match)
        if (_settings.EndpointOverrides.TryGetValue(path, out var exactOverride))
        {
            return exactOverride;
        }

        // Priority 2: Endpoint override (wildcard)
        foreach (var (pattern, maxSize) in _settings.EndpointOverrides)
        {
            if (pattern.EndsWith("*"))
            {
                var prefix = pattern[..^1]; // Remove the asterisk
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return maxSize;
                }
            }
        }

        // Priority 3: Export header
        if (httpContext.Request.Headers.TryGetValue("X-Export-Operation", out var exportHeader) &&
            exportHeader == "true")
        {
            return _settings.MaxExportPageSize;
        }

        // Priority 4: Role-based (highest if multiple roles)
        if (user?.Identity?.IsAuthenticated == true)
        {
            foreach (var (role, maxSize) in _settings.RoleBasedLimits.OrderByDescending(x => x.Value))
            {
                if (user.IsInRole(role))
                {
                    return maxSize;
                }
            }
        }

        // Priority 5: Default
        return _settings.MaxPageSize;
    }

    private static int ParseInt(string? value, int defaultValue, int min)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out var result))
        {
            return Math.Max(min, result);
        }

        return defaultValue;
    }
}
