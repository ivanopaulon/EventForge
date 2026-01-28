using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EventForge.Server.Filters;

/// <summary>
/// Filtro di validazione automatica che usa FluentValidation per validare i model.
/// Se la validazione fallisce, lancia una ValidationException che verrà gestita dal GlobalExceptionHandlerMiddleware.
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
        // Valida tutti i parametri dell'action
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
                        // Lancia ValidationException che verrà gestita dal GlobalExceptionHandlerMiddleware
                        throw new ValidationException(validationResult.Errors);
                    }
                }
            }
        }

        await next();
    }
}

/// <summary>
/// Extension methods per registrare FluentValidationFilter.
/// </summary>
public static class FluentValidationFilterExtensions
{
    /// <summary>
    /// Aggiunge il filtro di validazione FluentValidation ai controller.
    /// </summary>
    public static IMvcBuilder AddFluentValidationFilter(this IMvcBuilder builder)
    {
        builder.Services.AddScoped<FluentValidationFilter>();
        return builder;
    }
}
