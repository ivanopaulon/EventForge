using Microsoft.AspNetCore.Mvc.Filters;

namespace EventForge.Filters;

/// <summary>
/// Action filter for handling soft delete query parameters.
/// Adds support for 'deleted' query parameter with values: all, true, false
/// </summary>
public class SoftDeleteFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Query.TryGetValue("deleted", out var deletedValues))
        {
            var deletedValue = deletedValues.FirstOrDefault()?.ToLowerInvariant();

            // Store the deleted parameter value in HttpContext for use by services
            context.HttpContext.Items["IncludeDeleted"] = deletedValue switch
            {
                "all" => "all",
                "true" => "true",
                "false" => "false",
                _ => "false" // Default to excluding deleted items
            };
        }
        else
        {
            // Default behavior: exclude deleted items
            context.HttpContext.Items["IncludeDeleted"] = "false";
        }

        base.OnActionExecuting(context);
    }
}

/// <summary>
/// Helper class for working with soft delete filtering in services.
/// </summary>
public static class SoftDeleteHelper
{
    /// <summary>
    /// Gets the soft delete filter preference from HttpContext.
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <returns>The delete filter preference: "all", "true", or "false"</returns>
    public static string GetDeletedFilter(HttpContext? httpContext)
    {
        if (httpContext?.Items.TryGetValue("IncludeDeleted", out var value) == true)
        {
            return value?.ToString() ?? "false";
        }
        return "false";
    }

    /// <summary>
    /// Determines if deleted items should be included based on the filter.
    /// </summary>
    /// <param name="deletedFilter">The deleted filter value</param>
    /// <returns>True if deleted items should be included</returns>
    public static bool ShouldIncludeDeleted(string deletedFilter)
    {
        return deletedFilter is "all" or "true";
    }

    /// <summary>
    /// Determines if only deleted items should be returned.
    /// </summary>
    /// <param name="deletedFilter">The deleted filter value</param>
    /// <returns>True if only deleted items should be returned</returns>
    public static bool ShouldReturnOnlyDeleted(string deletedFilter)
    {
        return deletedFilter == "true";
    }
}