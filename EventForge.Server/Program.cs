using EventForge.Server.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using StackExchange.Profiling;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 🔍 STARTUP VALIDATION
// ========================================
var contentRoot = builder.Environment.ContentRootPath;
var environment = builder.Environment.EnvironmentName;

// Verify appsettings.json (CRITICAL)
var appsettingsPath = Path.Combine(contentRoot, "appsettings.json");
if (!File.Exists(appsettingsPath))
{
    throw new FileNotFoundException("Critical configuration file missing", "appsettings.json");
}

// Load environment-specific overrides from "Environments:{env}" section in appsettings.json.
// Development (launchSettings) → Port 7241 | Production (IIS default) → Port 7242
var envSection = builder.Configuration.GetSection($"Environments:{environment}");
if (envSection.Exists())
{
    var envOverrides = envSection
        .AsEnumerable(makePathsRelative: true)
        .Where(kvp => kvp.Value != null)
        .Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value));
    builder.Configuration.AddInMemoryCollection(envOverrides);
}

// Verify connection strings AFTER loading all configuration sources
var connectionStringsSection = builder.Configuration.GetSection("ConnectionStrings");
if (!connectionStringsSection.Exists())
{
    throw new InvalidOperationException("Configuration must contain ConnectionStrings section");
}

var allConnectionStrings = connectionStringsSection.GetChildren().ToList();
if (!allConnectionStrings.Any())
{
    throw new InvalidOperationException("Configuration must contain at least one connection string");
}

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServer");

if (string.IsNullOrEmpty(defaultConnection) && string.IsNullOrEmpty(sqlServerConnection))
{
    throw new InvalidOperationException("Required connection string missing");
}

// ========================================
// ✅ CONFIGURE LOGGING FIRST (CRITICAL!)
// ========================================
builder.AddCustomSerilogLogging();

// ========================================
// ✅ CONFIGURE SERVICES (WITH LOGGING ACTIVE)
// ========================================
builder.Services.AddConfiguredHttpClient(builder.Configuration);
builder.Services.AddConfiguredDbContext(builder.Configuration);

// Add Authentication & Authorization services
builder.Services.AddAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization(builder.Configuration);

// Note: Session state configuration (including IdleTimeout with sliding behavior) is handled
// in AddAuthentication extension method in ServiceCollectionExtensions.cs.
// Blazor WebAssembly uses JWT Bearer authentication, not cookie authentication, so
// ConfigureApplicationCookie is not applicable. The session IdleTimeout of 8 hours
// automatically provides sliding expiration behavior - it resets on each API request
// that accesses the session (e.g., tenant context operations).

// Add Health Checks
builder.Services.AddHealthChecks(builder.Configuration, builder.Environment);

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

// Note: Session and distributed cache are configured in AddAuthentication()
// in ServiceCollectionExtensions.cs with a 4-hour idle timeout.

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
builder.Services.AddScoped<EventForge.Server.Services.Configuration.IBrandingService, EventForge.Server.Services.Configuration.BrandingService>();

// Register Update services
builder.Services.AddSingleton<EventForge.Server.Services.Updates.IUpdateHubProxyService, EventForge.Server.Services.Updates.UpdateHubProxyService>();
builder.Services.AddSingleton<EventForge.Server.Services.Updates.IAgentUpdateProxyService, EventForge.Server.Services.Updates.AgentUpdateProxyService>();

// UpdatesAvailableRefreshService — broadcasts ReadyToDeploy package count to SuperAdmin clients
builder.Services.AddSingleton<EventForge.Server.Services.Updates.UpdatesAvailableRefreshService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EventForge.Server.Services.Updates.UpdatesAvailableRefreshService>());

// Agent monitor — singleton background service (probes Agent, auto-restarts if unreachable > threshold)
builder.Services.AddSingleton<EventForge.Server.Services.AgentMonitorService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EventForge.Server.Services.AgentMonitorService>());

// Register Dashboard services
builder.Services.AddScoped<EventForge.Server.Services.Dashboard.IServerStatusService, EventForge.Server.Services.Dashboard.ServerStatusService>();
builder.Services.AddScoped<EventForge.Server.Services.Dashboard.IPerformanceMetricsService, EventForge.Server.Services.Dashboard.PerformanceMetricsService>();

// Register Monitoring services (Sprint 4 — Fase 6 Optimization)
builder.Services.AddSingleton<EventForge.Server.Services.Monitoring.IMonitoringMetricsService, EventForge.Server.Services.Monitoring.MonitoringMetricsService>();
builder.Services.AddScoped<EventForge.Server.Services.Monitoring.IMonitoringService, EventForge.Server.Services.Monitoring.MonitoringService>();

// Register Hosted Services
builder.Services.AddHostedService<EventForge.Server.HostedServices.LogCleanupService>();
builder.Services.AddHostedService<EventForge.Server.HostedServices.PerformanceCollectorService>();
builder.Services.AddHostedService<EventForge.Server.HostedServices.DocumentLockCleanupWorker>();

// Add SignalR for real-time communication
builder.Services.AddSignalR(options =>
{
    // Configure SignalR options for better authentication support
    // Increased from 32KB to 256KB to accommodate HTML-formatted chat messages
    options.MaximumReceiveMessageSize = 256 * 1024; // 256KB
    options.StreamBufferCapacity = 10;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();

    // Configure timeouts to prevent slow connections (Issue #3)
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
})
.AddJsonProtocol(options =>
{
    // Use UnsafeRelaxedJsonEscaping so that HTML characters (<, >, &, etc.) in
    // chat message payloads are not double-escaped to \u003c / \u003e / \u0026.
    // This is safe because all HTML content is sanitized server-side by
    // HtmlSanitizerService before being persisted or broadcast.
    options.PayloadSerializerOptions.Encoder =
        System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// WhatsApp Cloud API named HttpClient
builder.Services.AddHttpClient("WhatsApp", client =>
{
    client.BaseAddress = new Uri("https://graph.facebook.com/");
    var accessToken = builder.Configuration["WhatsApp:AccessToken"] ?? string.Empty;
    if (!string.IsNullOrEmpty(accessToken))
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
});
builder.Services.AddScoped<EventForge.Server.Services.External.WhatsApp.IWhatsAppService, EventForge.Server.Services.External.WhatsApp.WhatsAppService>();
builder.Services.AddScoped<EventForge.Server.Services.External.WhatsApp.IWhatsAppConversazioneService, EventForge.Server.Services.External.WhatsApp.WhatsAppConversazioneService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PRYM API",
        Version = "v1.0.0",
        Description = "REST API for PRYM - Event management system with teams, audit logs and comprehensive business management features",
        Contact = new OpenApiContact
        {
            Name = "PRYM API Support",
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

    // Prefix each operationId with the controller name to avoid duplicates
    // (e.g. GetAll in multiple controllers). Same fix as Prym.ManagementHub.
    c.CustomOperationIds(d =>
        $"{d.ActionDescriptor.RouteValues["controller"]}_{d.ActionDescriptor.RouteValues["action"]}");

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
        // PrinterDto exists in both Prym.DTOs.Printing and Prym.DTOs.Station
        if (type.Name == "PrinterDto")
        {
            var namespacePrefix = type.Namespace?
                .Replace("Prym.DTOs.", "")
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

});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            // Default to localhost dev ports when no origins are configured
            allowedOrigins = ["https://localhost:7009", "http://localhost:5048"];
        }

        // Warn if the production placeholder was not replaced. This prevents silent CORS failures.
        var placeholder = "REPLACE_WITH_CLIENT_ORIGIN";
        if (Array.Exists(allowedOrigins, o => o.Contains(placeholder, StringComparison.OrdinalIgnoreCase)))
        {
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
            logger.LogWarning(
                "CORS configuration contains the placeholder '{Placeholder}'. " +
                "Replace it with the actual Client origin in appsettings.json or an environment override " +
                "before deploying to production — otherwise SignalR connections from the browser will be blocked.",
                placeholder);
        }

        _ = policy
            .WithOrigins(allowedOrigins)
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

// Register Bold Reports Community License key at startup
var boldReportsLicenseKey = builder.Configuration["BoldReportsLicenseKey"];
if (!string.IsNullOrWhiteSpace(boldReportsLicenseKey))
{
    Bold.Licensing.BoldLicenseProvider.RegisterLicense(boldReportsLicenseKey);
}

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

// Swagger is only enabled in Development or when explicitly enabled via Swagger:Enabled = true
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled", false))
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRYM API v1.0.0");
        c.RoutePrefix = "swagger"; // Swagger available at /swagger
        c.DocumentTitle = "PRYM API Documentation";
        c.DisplayRequestDuration();
    });
}

// Pipeline HTTP — exception handler first so wraps ALL subsequent middleware including SetupWizard
_ = app.UseGlobalExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    // Production hardening: HTTPS enforcement ONLY if binding HTTPS esiste.
    // Default false: non abilitare su server HTTP-only (IIS senza cert HTTPS).
    if (builder.Configuration.GetValue<bool>("Security:EnforceHttps", false))
    {
        _ = app.UseHsts();
        app.UseHttpsRedirection();
    }
}

// Setup wizard redirect — DOPO l'exception handler per catturare errori DB/Redis
app.UseMiddleware<EventForge.Server.Middleware.SetupWizardMiddleware>();

// Note: UseHttpsRedirection is conditionally used above based on environment and configuration

// Enable session BEFORE routing (required for session-dependent middleware)
app.UseSession();

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

// Redirect root and legacy paths to the dashboard.
// - "/"          → /Dashboard/Index  (requires SuperAdmin login)
// - "/settings*" → /Dashboard/Settings (legacy static panel used localStorage JWT
//                  never written by cookie-based login; must go through Razor Page)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null)
    {
        if (path == "/")
        {
            context.Response.Redirect("/Dashboard/Index", permanent: false);
            return;
        }

        if (path.Equals("/settings", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/settings/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/settings/index.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Dashboard/Settings", permanent: false);
            return;
        }
    }
    await next();
});

// Serve static files from wwwroot (Razor Pages assets)
app.UseStaticFiles();

// Authentication & Authorization
app.UseAuthentication();
// Push per-request context (CorrelationId, UserId, UserName, TenantId, RemoteIpAddress, RequestPath)
// into Serilog LogContext so dedicated SQL columns are populated on every log entry.
app.UseRequestContextEnricher();
app.UseCors();
#pragma warning disable ASP0001
app.UseAuthorization();
#pragma warning restore ASP0001

// Add authorization logging after authorization
app.UseAuthorizationLogging();

// Maintenance mode middleware (after authentication/authorization)
app.UseMiddleware<EventForge.Server.Middleware.MaintenanceMiddleware>();

// Map API Controllers
app.MapControllers();

// Map Razor Pages for server-side UI
app.MapRazorPages();

// Redirect /Dashboard to /Dashboard/Index for convenience
app.MapGet("/Dashboard", () => Results.Redirect("/Dashboard/Index"));

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

// SignalR hub endpoints.
// AppHub consolidates: notifications, audit-log, alerts, configuration and update-notifications.
// ChatHub, DocumentCollaborationHub and FiscalPrinterHub remain separate (complex group/lock management).
app.MapHub<EventForge.Server.Hubs.AppHub>("/hubs/app");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<DocumentCollaborationHub>("/hubs/document-collaboration");
app.MapHub<EventForge.Server.Hubs.FiscalPrinterHub>("/hubs/fiscal-printer");

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
