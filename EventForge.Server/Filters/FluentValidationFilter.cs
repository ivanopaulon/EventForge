using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace EventForge.Server.Filters;

/// <summary>
/// Automatic validation filter that uses FluentValidation to validate models.
/// If validation fails, logs full context and throws a ValidationException
/// that will be handled by GlobalExceptionHandlerMiddleware.
/// </summary>
public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FluentValidationFilter> _logger;

    public FluentValidationFilter(IServiceProvider serviceProvider, ILogger<FluentValidationFilter> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Validate all action parameters
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (context.ActionArguments.TryGetValue(parameter.Name, out var argument) && argument != null)
            {
                var argumentType = argument.GetType();
                var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

                var validator = _serviceProvider.GetService(validatorType) as IValidator;

                if (validator != null)
                {
                    var validationContext = new ValidationContext<object>(argument);
                    var validationResult = await validator.ValidateAsync(validationContext);

                    if (!validationResult.IsValid)
                    {
                        var httpContext = context.HttpContext;
                        var correlationId = httpContext.Items.TryGetValue("CorrelationId", out var cid) ? cid?.ToString() : "N/A";
                        var actionName = context.ActionDescriptor.DisplayName;
                        var queryString = httpContext.Request.QueryString.ToString();

                        var userName = httpContext.User?.Identity?.IsAuthenticated == true
                            ? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                            : null;
                        var userId   = httpContext.User?.FindFirst("user_id")?.Value;
                        var tenantId = httpContext.User?.FindFirst("tenant_id")?.Value;

                        var errorsByField = validationResult.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()
                            );

                        _logger.LogWarning(
                            "FluentValidation failed for {Method} {Path}{QueryString} (Action: {ActionName}, DtoType: {DtoType}) " +
                            "User: {UserName} (Id: {UserId}, Tenant: {TenantId}) CorrelationId: {CorrelationId}. " +
                            "Errors: {@ValidationErrors}",
                            httpContext.Request.Method,
                            httpContext.Request.Path,
                            queryString,
                            actionName,
                            argumentType.Name,
                            userName ?? "Anonymous",
                            userId ?? "N/A",
                            tenantId ?? "N/A",
                            correlationId,
                            errorsByField);

                        // Throws ValidationException which will be handled by GlobalExceptionHandlerMiddleware
                        throw new ValidationException(validationResult.Errors);
                    }
                }
            }
        }

        await next();
    }
}
