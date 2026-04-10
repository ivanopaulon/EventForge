using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventForge.Server.Filters;

/// <summary>
/// Automatic validation filter that uses FluentValidation to validate models.
/// If validation fails, logs full context and throws a ValidationException
/// that will be handled by GlobalExceptionHandlerMiddleware.
/// </summary>
public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
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
                        // Throw ValidationException immediately.
                        // GlobalExceptionHandlerMiddleware handles logging once with full request context
                        // (including CorrelationId, UserId, TenantId populated via RequestContextEnricherMiddleware).
                        // Logging here would produce a duplicate entry for every validation failure.
                        throw new ValidationException(validationResult.Errors);
                    }
                }
            }
        }

        await next();
    }
}
