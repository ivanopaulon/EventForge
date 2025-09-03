using EventForge.Server.Services.Banks;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Chat;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Events;
using EventForge.Server.Services.Licensing;
using EventForge.Server.Services.Logs;
using EventForge.Server.Services.Notifications;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Printing;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.RetailCart;
using EventForge.Server.Services.Station;
using EventForge.Server.Services.Store;
using EventForge.Server.Services.Teams;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.VatRates;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Text;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configura Serilog con fallback su file se il database non � disponibile.
    /// </summary>
    public static void AddCustomSerilogLogging(this WebApplicationBuilder builder)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .WriteTo.MSSqlServer(
                    connectionString: builder.Configuration.GetConnectionString("LogDb"),
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "Logs",
                        AutoCreateSqlTable = true,
                    })
                .CreateLogger();
            Log.Information("Serilog configurato per SQL Server.");
        }
        catch (Exception ex)
        {
            var filePath = builder.Configuration["Serilog:FilePath"] ?? "Logs/fallback-log-.log";
            var fileRetention = builder.Configuration.GetValue<int?>("Serilog:FileRetention") ?? 7;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .WriteTo.File(
                    path: filePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: fileRetention,
                    restrictedToMinimumLevel: LogEventLevel.Information
                )
                .CreateLogger();

            Log.Error(ex, "Errore nella configurazione del logging su SQL Server. Fallback su file.");
        }

        builder.Host.UseSerilog();
    }

    /// <summary>
    /// Configura HttpClient usando i parametri da appsettings.json con resilienza Polly.
    /// </summary>
    public static void AddConfiguredHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var httpClientBase = configuration["HttpClient:BaseAddress"] ?? "https://localhost";
        var httpClientPort = configuration.GetValue<int>("HttpClient:Port");
        var httpClientUri = new Uri($"{httpClientBase}:{httpClientPort}/");

        // Configure resilient HTTP client
        services.AddHttpClient("Default", client =>
        {
            client.BaseAddress = httpClientUri;
            client.Timeout = TimeSpan.FromSeconds(30); // Explicit timeout
        });

        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));
    }

    /// <summary>
    /// Configura il DbContext per SQL Server.
    /// </summary>
    public static void AddConfiguredDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("Configurazione DbContext: utilizzando SQL Server");

        // Register HTTP context accessor first for audit tracking
        services.AddHttpContextAccessor();

        // Register performance monitoring
        services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
        services.AddScoped<QueryPerformanceInterceptor>();

        try
        {
            services.AddDbContext<EventForgeDbContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("SqlServer"))
                       .AddInterceptors(serviceProvider.GetRequiredService<QueryPerformanceInterceptor>());
            });
            Log.Information("DbContext configurato per SQL Server.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Errore durante la configurazione del DbContext.");
            throw;
        }

        // Register audit services
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Register application log services
        services.AddScoped<IApplicationLogService, ApplicationLogService>();

        // Register unified log management service
        services.AddScoped<ILogManagementService, LogManagementService>();

        // Register notification and chat services - Step 3 SignalR Implementation
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IChatService, ChatService>();

        // Register team services
        services.AddScoped<ITeamService, TeamService>();

        // Register event services
        services.AddScoped<IEventService, EventService>();

        // Register bank services
        services.AddScoped<IBankService, BankService>();

        // Register unit of measure services
        services.AddScoped<IUMService, UMService>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();

        // Register VAT rate services
        services.AddScoped<IVatRateService, VatRateService>();

        // Register product services
        services.AddScoped<IProductService, ProductService>();

        // Register price list services
        services.AddScoped<IPriceListService, PriceListService>();

        // Register payment term services
        services.AddScoped<IPaymentTermService, PaymentTermService>();

        // Register store user services
        services.AddScoped<IStoreUserService, StoreUserService>();

        // Register station services
        services.AddScoped<IStationService, StationService>();

        // Register business party services
        services.AddScoped<IBusinessPartyService, BusinessPartyService>();

        // Register common services
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IClassificationNodeService, ClassificationNodeService>();
        services.AddScoped<IReferenceService, ReferenceService>();

        // Register warehouse services
        services.AddScoped<IStorageFacilityService, StorageFacilityService>();
        services.AddScoped<IStorageLocationService, StorageLocationService>();
        services.AddScoped<ILotService, LotService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ISerialService, SerialService>();

        // Register promotion services
        services.AddScoped<IPromotionService, PromotionService>();

        // Register retail cart session services
        services.AddScoped<IRetailCartSessionService, RetailCartSessionService>();

        // Register document services  
        services.AddScoped<IDocumentTypeService, DocumentTypeService>();
        services.AddScoped<IDocumentHeaderService, DocumentHeaderService>();
        services.AddScoped<IDocumentAttachmentService, DocumentAttachmentService>();
        services.AddScoped<IDocumentCommentService, DocumentCommentService>();
        services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();
        services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        services.AddScoped<IDocumentRecurrenceService, DocumentRecurrenceService>();

        // Register document analytics and supporting services
        services.AddScoped<IDocumentAnalyticsService, DocumentAnalyticsService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IAntivirusScanService, StubAntivirusScanService>();

        // TODO: Complete implementation for:
        // - Document services: DocumentRow, DocumentSummaryLink (create implementations)
        // - Document export, reminders, privacy, integrations services
        // - PromotionRule, PromotionRuleProduct services (create implementations)
    }

    /// <summary>
    /// Configures authentication services with JWT bearer token support.
    /// </summary>
    public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Register authentication services
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IBootstrapService, BootstrapService>();

        // Register tenant services
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantService, TenantService>();

        // Register SuperAdmin services
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<IBackupService, BackupService>();

        // Register licensing services
        services.AddScoped<ILicenseService, LicenseService>();

        // Register printing services
        services.AddScoped<EventForge.Server.Services.Interfaces.IQzPrintingService, QzPrintingService>();
        services.AddScoped<QzDigitalSignatureService>();
        
        // Register new QZ Tray services with environment variable support
        services.AddScoped<EventForge.Server.Services.QzSigner>();
        services.AddScoped<EventForge.Server.Services.QzWebSocketClient>();

        // Register barcode services
        services.AddScoped<EventForge.Server.Services.Interfaces.IBarcodeService, BarcodeService>();

        // Register hosted service for database migration and bootstrap
        services.AddHostedService<BootstrapHostedService>();

        // Configure session and distributed cache for tenant context
        // Use Redis in production environment, memory cache in development
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString) && !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Production: Use Redis for distributed caching and sessions
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "EventForge";
            });
            Log.Information("Redis distributed cache configured for production environment");
        }
        else
        {
            // Development: Use in-memory distributed cache
            services.AddDistributedMemoryCache();
            Log.Information("In-memory distributed cache configured for development environment");
        }

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(8); // Session timeout
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Name = "EventForge.Session";
        });

        // Get JWT configuration with environment variable support
        var jwtSection = configuration.GetSection("Authentication:Jwt");
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        // Try to get secret key from environment variable first, then fall back to configuration
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtOptions.SecretKey;

        if (string.IsNullOrEmpty(secretKey) || secretKey == "REPLACE_IN_PRODUCTION_WITH_ENVIRONMENT_VARIABLE")
        {
            throw new InvalidOperationException(
                "JWT SecretKey must be configured. Set JWT_SECRET_KEY environment variable or Authentication:Jwt:SecretKey in configuration. " +
                "For development, you can use a test key, but for production, use a secure randomly generated key of at least 32 characters.");
        }

        jwtOptions.SecretKey = secretKey;

        var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

        // Configure JWT authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(jwtOptions.ClockSkewMinutes)
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Read token from query parameter for SignalR connections
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/hubs/notifications") ||
                         path.StartsWithSegments("/hubs/chat") ||
                         path.StartsWithSegments("/hubs/audit-log")))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception?.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var username = context.Principal?.Identity?.Name;
                    Log.Debug("JWT token validated for user: {Username}", username);
                    return Task.CompletedTask;
                }
            };
        });

        Log.Information("JWT Authentication configured successfully");
    }

    /// <summary>
    /// Configures authorization services with policy-based authorization.
    /// </summary>
    public static void AddAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireUser", policy =>
                policy.RequireAuthenticatedUser())
            .AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin", "SuperAdmin")) // SuperAdmin can also access Admin content
            .AddPolicy("RequireManager", policy =>
                policy.RequireRole("Admin", "Manager", "SuperAdmin")) // SuperAdmin can access Manager content too
            .AddPolicy("RequireSuperAdmin", policy =>
                policy.RequireRole("SuperAdmin"))
            .AddPolicy("CanManageUsers", policy =>
                policy.RequireClaim("permission", "Users.Users.Create", "Users.Users.Update", "Users.Users.Delete"))
            .AddPolicy("CanViewReports", policy =>
                policy.RequireClaim("permission", "Reports.Reports.Read"))
            .AddPolicy("CanManageEvents", policy =>
                policy.RequireClaim("permission", "Events.Events.Create", "Events.Events.Update", "Events.Events.Delete"))
            .AddPolicy("AdminOrSuperAdmin", policy =>
                policy.RequireRole("Admin", "SuperAdmin")); // Explicit policy for Admin or SuperAdmin access

        Log.Information("Authorization policies configured successfully");
    }

    /// <summary>
    /// Configures ASP.NET Core health checks for monitoring application health.
    /// </summary>
    public static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<EventForgeDbContext>("database", tags: new[] { "ready" })
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "ready" });

        // Add SQL Server health check if connection string is available
        var connectionString = configuration.GetConnectionString("SqlServer");
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddHealthChecks()
                .AddSqlServer(connectionString, tags: new[] { "ready" });
        }

        // Add Redis health check if connection string is available (production)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString) && !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true)
        {
            services.AddHealthChecks()
                .AddRedis(redisConnectionString, "redis", tags: new[] { "ready" });
            Log.Information("Redis health check configured for production environment");
        }

        Log.Information("Health checks configured successfully");
    }

    /// <summary>
    /// Applica automaticamente le migrazioni e crea il database se necessario.
    /// Da chiamare all'avvio, dopo la build dell'app.
    /// </summary>
    public static void EnsureDatabaseMigrated(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();
        try
        {
            if (!db.Database.CanConnect())
            {
                Log.Error("Impossibile connettersi al database. Migrazione non eseguita.");
                return;
            }

            // Check for pending migrations before applying
            var pendingMigrations = db.Database.GetPendingMigrations().ToList();

            if (!pendingMigrations.Any())
            {
                Log.Information("Database è già aggiornato. Nessuna migrazione da applicare.");
                return;
            }

            Log.Information("Trovate {Count} migrazioni pendenti: {Migrations}",
                pendingMigrations.Count, string.Join(", ", pendingMigrations));

            db.Database.Migrate();

            Log.Information("Migrazioni applicate correttamente al database: {AppliedMigrations}",
                string.Join(", ", pendingMigrations));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Errore durante l'applicazione delle migrazioni al database.");
            throw;
        }
    }
}