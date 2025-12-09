using EventForge.Server.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Reflection;

// NOTE: Using Swashbuckle.AspNetCore 6.x with Microsoft.OpenApi 1.x for compatibility.
// Version 10.x uses Microsoft.OpenApi 2.x which has breaking changes.

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

    // JWT Bearer Authentication configuration
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Inserisci il token JWT come: Bearer {token}\nEsempio: \"Bearer eyJhbGciOi...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Register operation filter to apply security only to [Authorize] endpoints
    c.OperationFilter<EventForge.Server.Swagger.SwaggerAuthorizeCheckOperationFilter>();

    // TODO: Re-enable ProblemDetails schema mapping with Microsoft.OpenApi 2.x compatible syntax
    // TODO: Re-enable FileUploadOperationFilter with Microsoft.OpenApi 2.x compatible syntax
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        _ = policy
            .WithOrigins("https://localhost:7241", "http://localhost:7240", "https://localhost:5000", "https://localhost:7009") // aggiungi qui le porte del client Blazor se diverse
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR WebSocket connections
    });
});

// Add rate limiting for client log endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ClientLogs", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100, // 100 requests
                Window = TimeSpan.FromMinutes(1), // per minute
                SegmentsPerWindow = 6, // 6 segments of 10 seconds each
                QueueLimit = 10 // queue up to 10 requests
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded for client logging. Please retry later.",
            cancellationToken);
    };
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

// Enable rate limiting
app.UseRateLimiter();

// Serve default document (index.html) and static files from wwwroot
// UseDefaultFiles enables serving index.html when requesting the site root.
// MapFallbackToFile below ensures client-side routes are handled by index.html.
app.UseDefaultFiles();
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

// Map Health Checks endpoints (preserve existing ResponseWriter logic)
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

// Map SignalR hubs (preserve existing mappings)
app.MapHub<AuditLogHub>("/hubs/audit-log");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<DocumentCollaborationHub>("/hubs/document-collaboration");
app.MapHub<AlertHub>("/hubs/alerts");

// FALLBACK: serve index.html for any non-file, non-API route (SPA)
app.MapFallbackToFile("index.html");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
