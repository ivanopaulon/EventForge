using Bogus;
using EventForge.DTOs.DevTools;
using EventForge.Server.Data.Entities.Products;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace EventForge.Server.Services.DevTools;

/// <summary>
/// Servizio per la generazione di prodotti di test.
/// Utilizza Bogus per generare dati randomizzati e salvarli nel database.
/// </summary>
public class ProductGeneratorService : IProductGeneratorService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductGeneratorService> _logger;
    private readonly ConcurrentDictionary<string, GenerateProductsStatusDto> _jobStatuses = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobCancellationTokens = new();

    // Categorie predefinite per i prodotti
    private static readonly string[] Categories = new[]
    {
        "Elettronica", "Abbigliamento", "Casa e Giardino", "Sport", "Libri",
        "Giocattoli", "Alimentari", "Bellezza", "Auto e Moto", "Salute"
    };

    // Tag comuni per i prodotti
    private static readonly string[] Tags = new[]
    {
        "nuovo", "offerta", "bestseller", "limitato", "eco-friendly",
        "premium", "economico", "professionale", "domestico", "outdoor"
    };

    public ProductGeneratorService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductGeneratorService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> StartGenerationJobAsync(
        GenerateProductsRequestDto request,
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid().ToString();
        var status = new GenerateProductsStatusDto
        {
            JobId = jobId,
            Status = ProductGenerationJobStatus.Pending,
            Total = request.Count,
            StartedAt = DateTime.UtcNow
        };

        _jobStatuses[jobId] = status;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _jobCancellationTokens[jobId] = cts;

        // Avvia il job in background
        _ = Task.Run(async () =>
        {
            try
            {
                await GenerateProductsAsync(jobId, request, tenantId, userId, cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la generazione dei prodotti per il job {JobId}", jobId);
                UpdateJobStatus(jobId, ProductGenerationJobStatus.Failed, errorMessage: ex.Message);
            }
            finally
            {
                _jobCancellationTokens.TryRemove(jobId, out _);
            }
        }, cts.Token);

        return jobId;
    }

    public GenerateProductsStatusDto? GetJobStatus(string jobId)
    {
        return _jobStatuses.TryGetValue(jobId, out var status) ? status : null;
    }

    public bool CancelJob(string jobId)
    {
        if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            UpdateJobStatus(jobId, ProductGenerationJobStatus.Cancelled);
            return true;
        }
        return false;
    }

    private async Task GenerateProductsAsync(
        string jobId,
        GenerateProductsRequestDto request,
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        UpdateJobStatus(jobId, ProductGenerationJobStatus.Running);

        _logger.LogInformation("Avvio generazione di {Count} prodotti per il tenant {TenantId}", request.Count, tenantId);

        var faker = new Faker("it");
        var batchSize = request.BatchSize;
        var totalBatches = (int)Math.Ceiling((double)request.Count / batchSize);

        for (int batchIndex = 0; batchIndex < totalBatches && !cancellationToken.IsCancellationRequested; batchIndex++)
        {
            var currentBatchSize = Math.Min(batchSize, request.Count - (batchIndex * batchSize));
            await GenerateBatchAsync(jobId, faker, currentBatchSize, tenantId, userId, cancellationToken);

            // Piccola pausa tra i batch per non sovraccaricare il database
            if (batchIndex < totalBatches - 1)
            {
                await Task.Delay(50, cancellationToken);
            }
        }

        stopwatch.Stop();

        if (cancellationToken.IsCancellationRequested)
        {
            UpdateJobStatus(jobId, ProductGenerationJobStatus.Cancelled);
        }
        else
        {
            var status = _jobStatuses[jobId];
            status.Status = ProductGenerationJobStatus.Done;
            status.CompletedAt = DateTime.UtcNow;
            status.DurationSeconds = stopwatch.Elapsed.TotalSeconds;
            _logger.LogInformation("Completata generazione prodotti per job {JobId}. Creati: {Created}, Errori: {Errors}, Durata: {Duration}s",
                jobId, status.Created, status.Errors, status.DurationSeconds);
        }
    }

    private async Task GenerateBatchAsync(
        string jobId,
        Faker faker,
        int batchSize,
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EventForgeDbContext>();

        var products = new List<Product>();
        var productFaker = CreateProductFaker(faker, tenantId, userId);

        for (int i = 0; i < batchSize; i++)
        {
            try
            {
                var product = productFaker.Generate();
                products.Add(product);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore nella generazione di un prodotto nel batch per il job {JobId}", jobId);
                IncrementJobErrors(jobId);
            }
        }

        if (products.Any())
        {
            try
            {
                await dbContext.Products.AddRangeAsync(products, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                var status = _jobStatuses[jobId];
                status.Created += products.Count;
                status.Processed += batchSize;

                _logger.LogDebug("Batch salvato: {Count} prodotti per job {JobId}. Totale: {Total}/{Target}",
                    products.Count, jobId, status.Processed, status.Total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel salvataggio del batch per il job {JobId}", jobId);
                var errorCount = products.Count;
                IncrementJobErrors(jobId, errorCount);
                
                var status = _jobStatuses[jobId];
                status.Processed += batchSize;
            }
        }
    }

    private Faker<Product> CreateProductFaker(Faker faker, Guid tenantId, Guid userId)
    {
        var productFaker = new Faker<Product>("it")
            .CustomInstantiator(f => new Product())
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.ShortDescription, f => f.Commerce.ProductAdjective())
            .RuleFor(p => p.Description, f => f.Lorem.Sentence(10))
            .RuleFor(p => p.Code, f => $"TEST-{f.Random.AlphaNumeric(8).ToUpper()}")
            .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl(400, 400))
            .RuleFor(p => p.Status, f => f.PickRandom<EventForge.Server.Data.Entities.Products.ProductStatus>())
            .RuleFor(p => p.IsVatIncluded, f => f.Random.Bool())
            .RuleFor(p => p.DefaultPrice, f => f.Random.Decimal(1, 1000))
            .RuleFor(p => p.IsBundle, f => f.Random.Bool(0.1f)) // 10% sono bundle
            .RuleFor(p => p.ReorderPoint, f => f.Random.Decimal(1, 100))
            .RuleFor(p => p.SafetyStock, f => f.Random.Decimal(1, 50))
            .RuleFor(p => p.TargetStockLevel, f => f.Random.Decimal(50, 500))
            .RuleFor(p => p.AverageDailyDemand, f => f.Random.Decimal(1, 20))
            .RuleFor(p => p.TenantId, f => tenantId)
            .RuleFor(p => p.IsDeleted, f => false)
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(2))
            .RuleFor(p => p.CreatedBy, f => userId.ToString())
            .RuleFor(p => p.ModifiedAt, f => null)
            .RuleFor(p => p.ModifiedBy, f => null);

        return productFaker;
    }

    private void UpdateJobStatus(string jobId, ProductGenerationJobStatus status, string? errorMessage = null)
    {
        if (_jobStatuses.TryGetValue(jobId, out var jobStatus))
        {
            jobStatus.Status = status;
            if (errorMessage != null)
            {
                jobStatus.ErrorMessage = errorMessage;
            }
            if (status == ProductGenerationJobStatus.Done || status == ProductGenerationJobStatus.Failed || status == ProductGenerationJobStatus.Cancelled)
            {
                jobStatus.CompletedAt = DateTime.UtcNow;
                if (jobStatus.StartedAt.HasValue)
                {
                    jobStatus.DurationSeconds = (jobStatus.CompletedAt.Value - jobStatus.StartedAt.Value).TotalSeconds;
                }
            }
        }
    }

    private void IncrementJobErrors(string jobId, int count = 1)
    {
        if (_jobStatuses.TryGetValue(jobId, out var status))
        {
            status.Errors += count;
        }
    }
}
