using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventForge.Server.Swagger;

/// <summary>
/// Operation filter that adds security requirements to Swagger operations
/// that have the [Authorize] attribute at the action or controller level.
/// </summary>
public class SwaggerAuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the operation has [AllowAnonymous] attribute
        var actionAttrs = context.MethodInfo.GetCustomAttributes(true);
        if (actionAttrs.OfType<AllowAnonymousAttribute>().Any())
            return; // Skip endpoints explicitly marked as anonymous

        // Check if the operation has [Authorize] attribute
        var hasAuthorize = false;

        // Check action attributes
        if (actionAttrs.OfType<AuthorizeAttribute>().Any())
            hasAuthorize = true;

        // Check controller attributes
        var controllerType = context.MethodInfo.DeclaringType;
        if (!hasAuthorize && controllerType != null)
        {
            var controllerAttrs = controllerType.GetCustomAttributes(true);
            if (controllerAttrs.OfType<AuthorizeAttribute>().Any())
                hasAuthorize = true;
        }

        if (!hasAuthorize)
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        var bearerScheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [ bearerScheme ] = new string[] { }
        });
    }
}
