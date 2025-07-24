using EventForge.Services.Audit;
using EventForge.Services.Events;
using EventForge.Services.Teams;
using EventForge.Services.Banks;
using EventForge.Services.UnitOfMeasures;
using EventForge.Services.VatRates;
using EventForge.Services.PriceLists;
using EventForge.Services.Products;
using EventForge.Services.Business;
using EventForge.Services.Store;
using EventForge.Services.Station;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

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
    /// Configura HttpClient usando i parametri da appsettings.json.
    /// </summary>
    public static void AddConfiguredHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        var httpClientBase = configuration["HttpClient:BaseAddress"] ?? "https://localhost";
        var httpClientPort = configuration.GetValue<int>("HttpClient:Port");
        var httpClientUri = new Uri($"{httpClientBase}:{httpClientPort}/");

        services.AddHttpClient("Default", client =>
        {
            client.BaseAddress = httpClientUri;
        });
        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Default"));
    }

    /// <summary>
    /// Configura il DbContext per SQL Server o SQLite in base alla configurazione.
    /// </summary>
    public static void AddConfiguredDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"] ?? "SqlServer";
        Log.Information("Configurazione DbContext: provider selezionato = {Provider}", provider);

        try
        {
            if (provider == "Sqlite")
            {
                services.AddDbContext<EventForgeDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("Sqlite")));
                Log.Information("DbContext configurato per SQLite.");
            }
            else // Default: SQL Server
            {
                services.AddDbContext<EventForgeDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("SqlServer")));
                Log.Information("DbContext configurato per SQL Server.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Errore durante la configurazione del DbContext.");
            throw;
        }

        // Register audit services
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Register team services
        services.AddScoped<ITeamService, TeamService>();

        // Register event services
        services.AddScoped<IEventService, EventService>();

        // Register bank services
        services.AddScoped<IBankService, BankService>();

        // Register unit of measure services
        services.AddScoped<IUMService, UMService>();

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

        // TODO: Complete implementation for:
        // - IBusinessPartyService, BusinessPartyService (grouped service for BusinessParty + BusinessPartyAccounting)
        // - Common services: Address, Contact, ClassificationNode, Reference
        // - Warehouse services: StorageFacility, StorageLocation
        // - Promotion services: Promotion, PromotionRule, PromotionRuleProduct
        // - Document services: DocumentHeader, DocumentRow, DocumentSummaryLink, DocumentType
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

            db.Database.Migrate();
            Log.Information("Migrazioni applicate correttamente al database.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Errore durante l'applicazione delle migrazioni al database.");
            throw;
        }
    }
}