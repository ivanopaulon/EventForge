using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventForge.Server.Filters;

/// <summary>
/// Operation filter that applies security requirements to Swagger operations
/// that have the [Authorize] attribute at either the action or controller level.
/// </summary>
public class SwaggerAuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the action has [AllowAnonymous] attribute
        var hasAllowAnonymous = context.MethodInfo.GetCustomAttributes(true)
            .Any(attr => attr is AllowAnonymousAttribute);

        if (hasAllowAnonymous)
        {
            return; // Skip adding security requirement for anonymous endpoints
        }

        // Check if the action or controller has [Authorize] attribute
        var hasAuthorize = context.MethodInfo.GetCustomAttributes(true)
            .Any(attr => attr is AuthorizeAttribute) ||
            context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Any(attr => attr is AuthorizeAttribute) == true;

        if (hasAuthorize)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            };

            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(securityRequirement);
        }
    }
}
