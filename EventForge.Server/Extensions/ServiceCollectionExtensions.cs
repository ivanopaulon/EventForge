using EventForge.Server.Services.Alerts;
using EventForge.Server.Services.Auth.Seeders;
using EventForge.Server.Services.Banks;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.Chat;
using EventForge.Server.Services.CodeGeneration;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Dashboard;
using EventForge.Server.Services.DevTools;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Events;
using EventForge.Server.Services.External;
using EventForge.Server.Services.Licensing;
using EventForge.Server.Services.Logging;
using EventForge.Server.Services.Logs;
using EventForge.Server.Services.Notifications;
using EventForge.Server.Services.PriceHistory;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.PriceLists.Strategies;
using EventForge.Server.Services.Printing;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.RetailCart;
using EventForge.Server.Services.Sales;
using EventForge.Server.Services.Station;
using EventForge.Server.Services.Store;
using EventForge.Server.Services.Teams;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.VatRates;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        var filePath = builder.Configuration["Serilog:FilePath"] ?? "Logs/log-.log";
        var fileRetention = builder.Configuration.GetValue<int?>("Serilog:FileRetention") ?? 7;
        var logDbConnectionString = builder.Configuration.GetConnectionString("LogDb");

        // Configure column options for enriched properties
        var columnOptions = new ColumnOptions();

        // Add custom columns for client log enrichment
        columnOptions.AdditionalColumns = new System.Collections.ObjectModel.Collection<SqlColumn>
        {
            new SqlColumn { ColumnName = "Source", DataType = System.Data.SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
            new SqlColumn { ColumnName = "Page", DataType = System.Data.SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
            new SqlColumn { ColumnName = "UserAgent", DataType = System.Data.SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
            new SqlColumn { ColumnName = "ClientTimestamp", DataType = System.Data.SqlDbType.DateTimeOffset, AllowNull = true },
            new SqlColumn { ColumnName = "CorrelationId", DataType = System.Data.SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
            new SqlColumn { ColumnName = "Category", DataType = System.Data.SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
            new SqlColumn { ColumnName = "UserId", DataType = System.Data.SqlDbType.UniqueIdentifier, AllowNull = true },
            new SqlColumn { ColumnName = "UserName", DataType = System.Data.SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
            new SqlColumn { ColumnName = "RemoteIpAddress", DataType = System.Data.SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
            new SqlColumn { ColumnName = "RequestPath", DataType = System.Data.SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
            new SqlColumn { ColumnName = "ClientProperties", DataType = System.Data.SqlDbType.NVarChar, DataLength = -1, AllowNull = true }
        };

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Error)
            .Enrich.FromLogContext()  // Enable capturing scope properties
            .WriteTo.File(
                path: filePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: fileRetention,
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        // Add SQL Server sink if connection string is available
        // Test connection first before adding the sink to avoid partial configuration
        if (!string.IsNullOrEmpty(logDbConnectionString))
        {
            try
            {
                // Test the connection first
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(logDbConnectionString))
                {
                    connection.Open();
                }

                // Connection successful - add SQL Server sink
                _ = loggerConfiguration.WriteTo.MSSqlServer(
                    connectionString: logDbConnectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "Logs",
                        AutoCreateSqlTable = true
                    },
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    columnOptions: columnOptions);

                Log.Logger = loggerConfiguration.CreateLogger();
                Log.Information("Serilog configurato per SQL Server con enrichment, file e console logging.");
            }
            catch (Exception ex)
            {
                // If SQL Server connection fails, fall back to file and console logging only
                // Don't add SQL Server sink to configuration
                Log.Logger = loggerConfiguration.CreateLogger();
                Log.Warning(ex, "Impossibile connettersi al database per il logging. SQL Server logging disabilitato. Utilizzo file e console logging.");
            }
        }
        else
        {
            Log.Logger = loggerConfiguration.CreateLogger();
            Log.Warning("LogDb connection string non trovato. SQL Server logging disabilitato. Utilizzo file e console logging.");
        }

        _ = builder.Host.UseSerilog();
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
        _ = services.AddHttpClient("Default", client =>
        {
            client.BaseAddress = httpClientUri;
            client.Timeout = TimeSpan.FromSeconds(30); // Explicit timeout
        });

        _ = services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));

        // Configure simple VIES VAT validation service
        _ = services.AddHttpClient<IViesValidationService, ViesValidationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Configure VAT lookup service using VIES
        _ = services.AddScoped<IVatLookupService, VatLookupService>();
    }

    /// <summary>
    /// Configura il DbContext per SQL Server.
    /// </summary>
    public static void AddConfiguredDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information("Configurazione DbContext: utilizzando SQL Server");

        // Register HTTP context accessor first for audit tracking
        _ = services.AddHttpContextAccessor();

        // Register performance monitoring
        _ = services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
        _ = services.AddScoped<QueryPerformanceInterceptor>();

        try
        {
            _ = services.AddDbContext<EventForgeDbContext>((serviceProvider, options) =>
            {
                _ = options.UseSqlServer(configuration.GetConnectionString("SqlServer"))
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
        _ = services.AddScoped<IAuditLogService, AuditLogService>();

        // Register application log services
        _ = services.AddScoped<IApplicationLogService, ApplicationLogService>();

        // Register log sanitization service for public log viewing
        _ = services.AddScoped<ILogSanitizationService, LogSanitizationService>();

        // Register unified log management service
        _ = services.AddScoped<ILogManagementService, LogManagementService>();

        // Register log ingestion services for resilient client log processing
        _ = services.AddSingleton<LogIngestionService>();
        _ = services.AddSingleton<ILogIngestionService>(sp => sp.GetRequiredService<LogIngestionService>());
        _ = services.AddHostedService<LogIngestionBackgroundService>();

        // Register notification and chat services - Step 3 SignalR Implementation
        _ = services.AddScoped<INotificationService, NotificationService>();
        _ = services.AddScoped<IChatService, ChatService>();

        // Register team services
        _ = services.AddScoped<ITeamService, TeamService>();

        // Register event services
        _ = services.AddScoped<IEventService, EventService>();

        // Register bank services
        _ = services.AddScoped<IBankService, BankService>();

        // Register unit of measure services
        _ = services.AddScoped<IUMService, UMService>();
        _ = services.AddScoped<IUnitConversionService, UnitConversionService>();

        // Register VAT rate services
        _ = services.AddScoped<IVatRateService, VatRateService>();
        _ = services.AddScoped<IVatNatureService, VatNatureService>();

        // Register code generation services
        _ = services.AddScoped<IDailyCodeGenerator, DailySequentialCodeGenerator>();

        // Register product services
        _ = services.AddScoped<IProductService, ProductService>();
        _ = services.AddScoped<IBrandService, BrandService>();
        _ = services.AddScoped<IModelService, ModelService>();

        _ = services.AddScoped<ISupplierProductPriceHistoryService, SupplierProductPriceHistoryService>();
        _ = services.AddScoped<ISupplierProductBulkService, SupplierProductBulkService>();

        _ = services.AddScoped<ISupplierSuggestionService, SupplierSuggestionService>();

        _ = services.AddScoped<ISupplierProductCsvImportService, SupplierProductCsvImportService>();

        // Register DevTools services
        _ = services.AddSingleton<IProductGeneratorService, ProductGeneratorService>();

        // Register alert services
        _ = services.AddScoped<ISupplierPriceAlertService, SupplierPriceAlertService>();



        // Register price list services (refactored into specialized services)
        _ = services.AddScoped<IPriceListService, PriceListService>();
        _ = services.AddScoped<IPriceListGenerationService, PriceListGenerationService>();
        _ = services.AddScoped<IPriceCalculationService, PriceCalculationService>();
        _ = services.AddScoped<IPriceListBusinessPartyService, PriceListBusinessPartyService>();
        _ = services.AddScoped<IPriceListBulkOperationsService, PriceListBulkOperationsService>();

        // Register price precedence strategy
        _ = services.AddScoped<IPricePrecedenceStrategy, DefaultPricePrecedenceStrategy>();

        // Register payment term services
        _ = services.AddScoped<IPaymentTermService, PaymentTermService>();

        // Register sales services
        _ = services.AddScoped<IPaymentMethodService, PaymentMethodService>();
        _ = services.AddScoped<ISaleSessionService, SaleSessionService>();
        _ = services.AddScoped<INoteFlagService, NoteFlagService>();
        _ = services.AddScoped<ITableManagementService, TableManagementService>();

        // Register store user services
        _ = services.AddScoped<IStoreUserService, StoreUserService>();

        // Register station services
        _ = services.AddScoped<IStationService, StationService>();

        // Register business party services
        _ = services.AddScoped<IBusinessPartyService, BusinessPartyService>();

        // Register common services
        _ = services.AddScoped<IAddressService, AddressService>();
        _ = services.AddScoped<IContactService, ContactService>();
        _ = services.AddScoped<IClassificationNodeService, ClassificationNodeService>();
        _ = services.AddScoped<IReferenceService, ReferenceService>();

        // Register warehouse services
        _ = services.AddScoped<IStorageFacilityService, StorageFacilityService>();
        _ = services.AddScoped<IStorageLocationService, StorageLocationService>();
        _ = services.AddScoped<ILotService, LotService>();
        _ = services.AddScoped<IStockService, StockService>();
        _ = services.AddScoped<ISerialService, SerialService>();
        _ = services.AddScoped<IStockMovementService, StockMovementService>();
        _ = services.AddScoped<IStockAlertService, StockAlertService>();
        _ = services.AddScoped<ITransferOrderService, TransferOrderService>();
        _ = services.AddScoped<IInventoryBulkSeedService, InventoryBulkSeedService>();
        _ = services.AddScoped<IInventoryDiagnosticService, InventoryDiagnosticService>();

        // Register promotion services
        _ = services.AddScoped<IPromotionService, PromotionService>();

        // Register retail cart session services
        _ = services.AddScoped<IRetailCartSessionService, RetailCartSessionService>();

        // Register document services  
        _ = services.AddScoped<IDocumentTypeService, DocumentTypeService>();
        _ = services.AddScoped<IDocumentCounterService, DocumentCounterService>();
        _ = services.AddScoped<IDocumentHeaderService, DocumentHeaderService>();
        _ = services.AddScoped<IDocumentAttachmentService, DocumentAttachmentService>();
        _ = services.AddScoped<IDocumentCommentService, DocumentCommentService>();
        _ = services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();
        _ = services.AddScoped<IDocumentWorkflowService, DocumentWorkflowService>();
        _ = services.AddScoped<IDocumentRecurrenceService, DocumentRecurrenceService>();
        _ = services.AddScoped<IDocumentStatusService, DocumentStatusService>();

        // Register document analytics and supporting services
        _ = services.AddScoped<IDocumentAnalyticsService, DocumentAnalyticsService>();
        _ = services.AddScoped<IFileStorageService, LocalFileStorageService>();
        _ = services.AddScoped<IAntivirusScanService, StubAntivirusScanService>();

        // Register document export and management services
        _ = services.AddScoped<IDocumentExportService, DocumentExportService>();
        _ = services.AddScoped<IDocumentRetentionService, DocumentRetentionService>();
        _ = services.AddScoped<IDocumentAccessLogService, DocumentAccessLogService>();

        // Register Excel export service
        _ = services.AddScoped<IExcelExportService, ExcelExportService>();

        // Register document facade for unified API access
        _ = services.AddScoped<IDocumentFacade, DocumentFacade>();

        // Register dashboard configuration services
        _ = services.AddScoped<IDashboardConfigurationService, DashboardConfigurationService>();

        // TODO: Complete implementation for:
        // - Document services: DocumentRow, DocumentSummaryLink (create implementations)
        // - Document reminders, privacy, integrations services
        // - PromotionRule, PromotionRuleProduct services (create implementations)
    }

    /// <summary>
    /// Configures authentication services with JWT bearer token support.
    /// </summary>
    public static void AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Register authentication services
        _ = services.AddScoped<IPasswordService, PasswordService>();
        _ = services.AddScoped<IJwtTokenService, JwtTokenService>();
        _ = services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Register bootstrap and seeder services
        _ = services.AddScoped<IBootstrapService, BootstrapService>();
        _ = services.AddScoped<IUserSeeder, UserSeeder>();
        _ = services.AddScoped<ITenantSeeder, TenantSeeder>();
        _ = services.AddScoped<ILicenseSeeder, LicenseSeeder>();
        _ = services.AddScoped<IEntitySeeder, EntitySeeder>();
        _ = services.AddScoped<IProductSeeder, ProductSeeder>();
        _ = services.AddScoped<IStoreSeeder, StoreSeeder>();

        // Register tenant services
        _ = services.AddScoped<ITenantContext, TenantContext>();
        _ = services.AddScoped<ITenantService, TenantService>();
        _ = services.AddScoped<ITenantUserManagementService, TenantUserManagementService>();

        // Register SuperAdmin services
        _ = services.AddScoped<IConfigurationService, ConfigurationService>();
        _ = services.AddScoped<IBackupService, BackupService>();

        // Register licensing services
        _ = services.AddScoped<ILicenseService, LicenseService>();

        // Register printing services
        _ = services.AddScoped<EventForge.Server.Services.Interfaces.IQzPrintingService, QzPrintingService>();
        _ = services.AddScoped<QzDigitalSignatureService>();

        // Register new QZ Tray services with environment variable support
        _ = services.AddScoped<EventForge.Server.Services.QzSigner>();
        _ = services.AddScoped<EventForge.Server.Services.QzWebSocketClient>();

        // Register barcode services
        _ = services.AddScoped<EventForge.Server.Services.Interfaces.IBarcodeService, BarcodeService>();

        // Register hosted service for database migration and bootstrap
        _ = services.AddHostedService<BootstrapHostedService>();

        // Configure session and distributed cache for tenant context
        // Use Redis in production environment, memory cache in development
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString) && !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Production: Use Redis for distributed caching and sessions
            _ = services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "EventForge";
            });
            Log.Information("Redis distributed cache configured for production environment");
        }
        else
        {
            // Development: Use in-memory distributed cache
            _ = services.AddDistributedMemoryCache();
            Log.Information("In-memory distributed cache configured for development environment");
        }

        _ = services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(8); // Session timeout aligned with cookie expiration
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
        _ = services.AddAuthentication(options =>
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
        // Authorization handlers that depend on scoped services (DbContext, TenantContext) 
        // MUST be registered as Scoped, not Singleton. The handler will be instantiated per request.
        _ = services.AddScoped<IAuthorizationHandler, EventForge.Server.Auth.TenantAdminAuthorizationHandler>();

        _ = services.AddAuthorizationBuilder()
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
                policy.RequireRole("Admin", "SuperAdmin")) // Explicit policy for Admin or SuperAdmin access
            .AddPolicy("RequireTenantAdmin", policy =>
                policy.Requirements.Add(new EventForge.Server.Auth.TenantAdminRequirement(EventForge.DTOs.Common.AdminAccessLevel.TenantAdmin)))
            .AddPolicy("RequireTenantFullAccess", policy =>
                policy.Requirements.Add(new EventForge.Server.Auth.TenantAdminRequirement(EventForge.DTOs.Common.AdminAccessLevel.FullAccess)));

        Log.Information("Authorization policies configured successfully");
    }

    /// <summary>
    /// Configures ASP.NET Core health checks for monitoring application health.
    /// Optimized: Health checks don't probe on registration, only when endpoint is called.
    /// </summary>
    public static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // OPTIMIZATION: Health checks are lazy - they don't probe during registration
        // They only execute when /health endpoint is called
        _ = services.AddHealthChecks()
            .AddDbContextCheck<EventForgeDbContext>("database", tags: new[] { "ready" })
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), tags: new[] { "ready" });

        // Add SQL Server health check if connection string is available
        var connectionString = configuration.GetConnectionString("SqlServer");
        if (!string.IsNullOrEmpty(connectionString))
        {
            _ = services.AddHealthChecks()
                .AddSqlServer(connectionString, tags: new[] { "ready" });
        }

        // Add Redis health check if connection string is available (production)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString) && !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true)
        {
            _ = services.AddHealthChecks()
                .AddRedis(redisConnectionString, "redis", tags: new[] { "ready" });
            Log.Information("Redis health check configured for production environment");
        }

        Log.Information("Health checks configured successfully - will probe on first request");
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