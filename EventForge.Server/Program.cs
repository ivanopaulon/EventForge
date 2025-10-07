using EventForge.Server.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
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

// Add Health Checks
builder.Services.AddHealthChecks(builder.Configuration);

// Add API Controllers support
builder.Services.AddControllers();

// Add Memory Cache for performance optimizations
builder.Services.AddMemoryCache();

// Add SignalR for real-time communication
builder.Services.AddSignalR(options =>
{
    // Configure SignalR options for better authentication support
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
    options.StreamBufferCapacity = 10;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

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

    // Configure custom schema IDs to avoid conflicts between classes with the same name
    c.CustomSchemaIds(type => type.FullName);

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

    // Supporto per upload file
    c.OperationFilter<FileUploadOperationFilter>();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        _ = policy
            .WithOrigins("https://localhost:7241", "https://localhost:5000", "https://localhost:7009") // aggiungi qui le porte del client Blazor se diverse
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR WebSocket connections
    });
});

var app = builder.Build();

// Add middleware early in the pipeline
app.UseCorrelationId();

// Add startup performance monitoring (logs time to first request)
app.UseStartupPerformanceMonitoring();

// Configure environment-aware homepage and Swagger behavior
if (app.Environment.IsDevelopment())
{
    // Development: Enable Swagger and set as homepage
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventForge API v1.0.0");
        c.RoutePrefix = string.Empty; // Set Swagger as the homepage
        c.DocumentTitle = "EventForge API Documentation";
        c.DisplayRequestDuration();
    });
}
else
{
    // Production: Enable Swagger but redirect homepage to logs viewer
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventForge API v1.0.0");
        c.RoutePrefix = "swagger"; // Swagger available at /swagger
        c.DocumentTitle = "EventForge API Documentation";
        c.DisplayRequestDuration();
    });
}

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    // Use our custom ProblemDetails middleware instead
    _ = app.UseProblemDetails();
    _ = app.UseHsts();
}
else
{
    _ = app.UseProblemDetails(); // Use our middleware in all environments
}

app.UseHttpsRedirection();

// Enable routing BEFORE static files
app.UseRouting();

// Explicitly disable default files middleware to prevent index.html fallback
// app.UseDefaultFiles(); // NOT USING THIS

// Serve static files (for uploaded images) but don't use default files
app.UseStaticFiles();

// Enable session support for tenant context
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseCors();
app.UseAuthorization();

// Add authorization logging after authorization
app.UseAuthorizationLogging();

// Map API Controllers
app.MapControllers();

// Map Health Checks endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Map SignalR hubs
app.MapHub<AuditLogHub>("/hubs/audit-log");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<DocumentCollaborationHub>("/hubs/document-collaboration");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
