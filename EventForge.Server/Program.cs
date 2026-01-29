using EventForge.Server.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using StackExchange.Profiling;
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

// Note: Session state configuration (including IdleTimeout with sliding behavior) is handled
// in AddAuthentication extension method in ServiceCollectionExtensions.cs.
// Blazor WebAssembly uses JWT Bearer authentication, not cookie authentication, so
// ConfigureApplicationCookie is not applicable. The session IdleTimeout of 8 hours
// automatically provides sliding expiration behavior - it resets on each API request
// that accesses the session (e.g., tenant context operations).

// Add Health Checks
builder.Services.AddHealthChecks(builder.Configuration);

// Configure Pagination Settings
builder.Services.Configure<EventForge.Server.Configuration.PaginationSettings>(
    builder.Configuration.GetSection(EventForge.Server.Configuration.PaginationSettings.SectionName));

// Add API Controllers support
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new EventForge.Server.ModelBinders.PaginationModelBinderProvider());
    // Add FluentValidation filter
    options.Filters.Add<EventForge.Server.Filters.FluentValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = 
        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Add Razor Pages for server-side UI (Setup Wizard & Dashboard)
builder.Services.AddRazorPages(options =>
{
    // Allow anonymous access to setup pages
    options.Conventions.AllowAnonymousToPage("/Setup/Index");
    options.Conventions.AllowAnonymousToPage("/Setup/Complete");
    // Allow anonymous access to server auth pages
    options.Conventions.AllowAnonymousToPage("/ServerAuth/Login");
    // Require SuperAdmin role for dashboard pages
    options.Conventions.AuthorizeFolder("/Dashboard", "RequireSuperAdmin");
});

// Add session support for wizard state
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Configure Memory Cache for performance optimizations with size limits
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;  // Maximum 1024 cache entries
    // CompactionPercentage: when cache is full, remove 25% of entries based on priority
    options.CompactionPercentage = 0.25;
});

// Register cache service
builder.Services.AddSingleton<EventForge.Server.Services.Caching.ICacheService, EventForge.Server.Services.Caching.CacheService>();

// Register cache invalidation service for Output Cache
builder.Services.AddScoped<EventForge.Server.Services.Caching.ICacheInvalidationService, EventForge.Server.Services.Caching.CacheInvalidationService>();

// Register Setup Wizard services
builder.Services.AddScoped<EventForge.Server.Services.Setup.IFirstRunDetectionService, EventForge.Server.Services.Setup.FirstRunDetectionService>();
builder.Services.AddScoped<EventForge.Server.Services.Setup.ISqlServerDiscoveryService, EventForge.Server.Services.Setup.SqlServerDiscoveryService>();
builder.Services.AddScoped<EventForge.Server.Services.Setup.ISetupWizardService, EventForge.Server.Services.Setup.SetupWizardService>();

// Register Configuration services
builder.Services.AddScoped<EventForge.Server.Services.Configuration.IPortConfigurationService, EventForge.Server.Services.Configuration.PortConfigurationService>();

// Register Dashboard services
builder.Services.AddScoped<EventForge.Server.Services.Dashboard.IServerStatusService, EventForge.Server.Services.Dashboard.ServerStatusService>();
builder.Services.AddScoped<EventForge.Server.Services.Dashboard.IPerformanceMetricsService, EventForge.Server.Services.Dashboard.PerformanceMetricsService>();

// Register Hosted Services
builder.Services.AddHostedService<EventForge.Server.HostedServices.LogCleanupService>();
builder.Services.AddHostedService<EventForge.Server.HostedServices.PerformanceCollectorService>();

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

    // Configure custom schema IDs - simplified for better Swagger UI readability
    // Uses simple class names for non-generic types; for generic types, creates
    // concatenated names from the generic type and its arguments (e.g., PagedResultOfProductDto)
    c.CustomSchemaIds(type => GetSchemaId(type));

    static string GetSchemaId(Type type)
    {
        // Handle generic types recursively (e.g., PagedResult<T>, ActionResult<PagedResult<T>>)
        if (type.IsGenericType)
        {
            var genericName = type.Name.Split('`')[0];
            var genericArgs = string.Join("", type.GetGenericArguments().Select(t => GetSchemaId(t)));
            return $"{genericName}Of{genericArgs}";
        }
        
        // Known conflicting types: use namespace prefix to avoid Swagger schema conflicts
        // PrinterDto exists in both EventForge.DTOs.Printing and EventForge.DTOs.Station
        if (type.Name == "PrinterDto")
        {
            var namespacePrefix = type.Namespace?
                .Replace("EventForge.DTOs.", "")
                .Replace(".", "") ?? "";
            return $"{namespacePrefix}{type.Name}";
        }
        
        // For non-generic, non-conflicting types, use simple class name
        // This makes Swagger UI much more readable and performant
        return type.Name;
    }

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

// Add rate limiting for client log endpoints and production hardening
builder.Services.AddRateLimiter(options =>
{
    // Client logs endpoint rate limiting
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

    // Production hardening: Login rate limiting (5 attempts per 5 minutes)
    options.AddPolicy("login", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginLimit", 5),
                Window = TimeSpan.FromMinutes(5),
                SegmentsPerWindow = 5,
                QueueLimit = 0
            }));

    // Production hardening: API rate limiting (100 calls per minute)
    options.AddPolicy("api", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:ApiLimit", 100),
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Production hardening: Token refresh rate limiting (1 per minute)
    options.AddPolicy("token-refresh", context =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:TokenRefreshLimit", 1),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Global limiter by IP as fallback
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(ipAddress, _ =>
            new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please retry later.",
            cancellationToken);
    };
});

// Add MiniProfiler for performance profiling
builder.Services.AddMiniProfiler(options =>
{
    // UI configuration
    options.RouteBasePath = "/profiler";
    options.PopupRenderPosition = StackExchange.Profiling.RenderPosition.BottomLeft;
    options.PopupShowTimeWithChildren = true;
    options.PopupShowTrivial = true;
    
    // Performance
    options.EnableServerTimingHeader = true;
    options.TrackConnectionOpenClose = true;
    
    // Storage uses default in-memory cache
    // options.Storage is set automatically to use memory cache
    
    // Ignore paths
    options.IgnoredPaths.Add("/swagger");
    options.IgnoredPaths.Add("/health");
}).AddEntityFramework();

// Output Cache configuration
builder.Services.AddOutputCache(options =>
{
    // Policy 1: Static Entities (1 hour cache)
    // For: VatRates, DocumentTypes, PaymentTerms, Banks
    options.AddPolicy("StaticEntities", builder => 
        builder
            .Expire(TimeSpan.FromHours(1))
            .SetVaryByQuery("page", "pageSize")
            .Tag("static"));
    
    // Policy 2: Semi-Static Entities (10 minutes cache)
    // For: UnitOfMeasures, Brands, Models, PaymentMethods, ClassificationNodes, NoteFlags
    options.AddPolicy("SemiStaticEntities", builder =>
        builder
            .Expire(TimeSpan.FromMinutes(10))
            .SetVaryByQuery("page", "pageSize")
            .Tag("semi-static"));
    
    // Policy 3: Real-Time Short Cache (5 seconds)
    // For: POS.GetOpenSessions, Tables.GetAvailableTables
    options.AddPolicy("RealTimeShortCache", builder =>
        builder
            .Expire(TimeSpan.FromSeconds(5))
            .SetVaryByQuery("page", "pageSize")
            .Tag("realtime"));
});

var app = builder.Build();

// Validate DI configuration at startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();

    EventForge.Server.Startup.DependencyValidationService.ValidateDependencies(
        scope.ServiceProvider,
        logger);
}

// Add middleware early in the pipeline
app.UseCorrelationId();

// Add startup performance monitoring (logs time to first request)
app.UseStartupPerformanceMonitoring();

// Setup wizard redirect (before routing) - check if first run
app.UseMiddleware<EventForge.Server.Middleware.SetupWizardMiddleware>();

// Configure environment-aware Swagger behavior with protection in production
if (app.Environment.IsDevelopment())
{
    // Development: Enable Swagger at /swagger (publicly accessible)
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventForge API v1.0.0");
        c.RoutePrefix = "swagger"; // Swagger available at /swagger
        c.DocumentTitle = "EventForge API Documentation";
        c.DisplayRequestDuration();
    });
}
else
{
    // Production: Protect Swagger with authentication - SuperAdmin only
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/swagger"),
        appBuilder =>
        {
            appBuilder.Use(async (context, next) =>
            {
                // Check authentication
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    context.Response.Redirect("/server/login?returnUrl=/swagger");
                    return;
                }
                
                // Check SuperAdmin role
                if (!context.User.IsInRole("SuperAdmin"))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Swagger access requires SuperAdmin role");
                    return;
                }
                
                await next();
            });
        });
    
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
    // Use global exception handler middleware for centralized exception handling
    _ = app.UseGlobalExceptionHandler();
    
    // Production hardening: HTTPS enforcement
    if (builder.Configuration.GetValue<bool>("Security:EnforceHttps", true))
    {
        _ = app.UseHsts();
        app.UseHttpsRedirection();
    }
}
else
{
    _ = app.UseGlobalExceptionHandler(); // Use global exception handler in all environments
}

// Note: UseHttpsRedirection is conditionally used above based on environment and configuration

// Enable routing BEFORE static files
app.UseRouting();

// Enable Output Cache middleware
app.UseOutputCache();

// Add MiniProfiler middleware for performance profiling
app.UseMiniProfiler();

// Add performance telemetry middleware
app.UsePerformanceTelemetry();

// Enable rate limiting
app.UseRateLimiter();

// Serve default document (index.html) and static files from wwwroot
// UseDefaultFiles enables serving index.html when requesting the site root.
// MapFallbackToFile below ensures client-side routes are handled by index.html.
app.UseDefaultFiles();
app.UseStaticFiles();

// Enable session support for wizard state
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseCors();
app.UseAuthorization();

// Add authorization logging after authorization
app.UseAuthorizationLogging();

// Maintenance mode middleware (after authentication/authorization)
app.UseMiddleware<EventForge.Server.Middleware.MaintenanceMiddleware>();

// Map API Controllers
app.MapControllers();

// Map Razor Pages for server-side UI
app.MapRazorPages();

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
app.MapHub<EventForge.Server.Hubs.ConfigurationHub>("/hubs/configuration");

// FALLBACK: serve index.html for any non-file, non-API route (SPA)
app.MapFallbackToFile("index.html");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
