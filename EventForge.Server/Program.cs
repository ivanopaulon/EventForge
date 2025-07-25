using EventForge.Server.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// builder.AddCustomSerilogLogging();
builder.Services.AddConfiguredHttpClient(builder.Configuration);
builder.Services.AddConfiguredDbContext(builder.Configuration);
builder.AddCustomSerilogLogging();

// Add Authentication & Authorization services
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddAuthorization(builder.Configuration);

// Add API Controllers support
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventForge API",
        Version = "v1.0.0",
        Description = "REST API for EventForge - Event management system with teams, audit logs and comprehensive business management features",
        Contact = new OpenApiContact
        {
            Name = "EventForge API Support",
            Email = "support@eventforge.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add ProblemDetails schema examples
    c.MapType<ProblemDetails>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "object",
        Properties = new Dictionary<string, OpenApiSchema>
        {
            ["type"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("https://tools.ietf.org/html/rfc7231#section-6.5.1") },
            ["title"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("One or more validation errors occurred.") },
            ["status"] = new() { Type = "integer", Example = new Microsoft.OpenApi.Any.OpenApiInteger(400) },
            ["detail"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("The input was not valid.") },
            ["instance"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("/api/v1/events") },
            ["correlationId"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("12345678-1234-1234-1234-123456789012") },
            ["timestamp"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("2024-01-01T12:00:00Z") }
        }
    });

    // Add validation problem details schema
    c.MapType<ValidationProblemDetails>(() => new OpenApiSchema
    {
        Type = "object",
        Properties = new Dictionary<string, OpenApiSchema>
        {
            ["type"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("https://tools.ietf.org/html/rfc7231#section-6.5.1") },
            ["title"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("One or more validation errors occurred.") },
            ["status"] = new() { Type = "integer", Example = new Microsoft.OpenApi.Any.OpenApiInteger(400) },
            ["detail"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("See the errors property for details.") },
            ["instance"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("/api/v1/events") },
            ["errors"] = new()
            {
                Type = "object",
                AdditionalProperties = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Type = "string" }
                },
                Example = new Microsoft.OpenApi.Any.OpenApiObject
                {
                    ["Name"] = new Microsoft.OpenApi.Any.OpenApiArray
                    {
                        new Microsoft.OpenApi.Any.OpenApiString("The Name field is required.")
                    }
                }
            },
            ["correlationId"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("12345678-1234-1234-1234-123456789012") },
            ["timestamp"] = new() { Type = "string", Example = new Microsoft.OpenApi.Any.OpenApiString("2024-01-01T12:00:00Z") }
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    });
});

var app = builder.Build();

app.Services.EnsureDatabaseMigrated(); // opzionale, per applicare le migrazioni

// Bootstrap admin user and permissions
using (var scope = app.Services.CreateScope())
{
    var bootstrapService = scope.ServiceProvider.GetRequiredService<IBootstrapService>();
    await bootstrapService.EnsureAdminBootstrappedAsync();
}

// Add middleware early in the pipeline
app.UseCorrelationId();

// Configure Swagger (in all environments for now, but you might want to restrict to development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventForge API v1.0.0");
    c.RoutePrefix = string.Empty; // Set Swagger as the homepage
    c.DocumentTitle = "EventForge API Documentation";
    c.DisplayRequestDuration();
});

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    // Use our custom ProblemDetails middleware instead
    app.UseProblemDetails();
    app.UseHsts();
}
else
{
    app.UseProblemDetails(); // Use our middleware in all environments
}

app.UseHttpsRedirection();

// Serve static files (for uploaded images)
app.UseStaticFiles();

// Enable session support for tenant context
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map API Controllers
app.MapControllers();

app.Run();
