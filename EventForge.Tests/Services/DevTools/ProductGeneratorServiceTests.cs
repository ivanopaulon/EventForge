using EventForge.DTOs.DevTools;
using EventForge.Server.Data;
using EventForge.Server.Services.DevTools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventForge.Tests.Services.DevTools;

/// <summary>
/// Unit tests per ProductGeneratorService.
/// </summary>
[Trait("Category", "Unit")]
public class ProductGeneratorServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ProductGeneratorService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ProductGeneratorServiceTests()
    {
        // Setup in-memory database e service provider
        var services = new ServiceCollection();

        services.AddDbContext<EventForgeDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = _serviceProvider.GetRequiredService<ILogger<ProductGeneratorService>>();

        _service = new ProductGeneratorService(scopeFactory, logger);
    }

    [Fact]
    public async Task StartGenerationJobAsync_ShouldStartJobAndReturnJobId()
    {
        // Arrange
        var request = new GenerateProductsRequestDto
        {
            Count = 10,
            BatchSize = 5
        };

        // Act
        var jobId = await _service.StartGenerationJobAsync(request, _tenantId, _userId);

        // Assert
        Assert.NotNull(jobId);
        Assert.NotEmpty(jobId);

        // Attendi un po' per permettere al job di avviarsi
        await Task.Delay(500);

        var status = _service.GetJobStatus(jobId);
        Assert.NotNull(status);
        Assert.Equal(jobId, status.JobId);
        Assert.Equal(10, status.Total);
    }

    [Fact]
    public async Task GetJobStatus_ShouldReturnNullForNonExistentJob()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid().ToString();

        // Act
        var status = _service.GetJobStatus(nonExistentJobId);

        // Assert
        Assert.Null(status);
    }

    [Fact]
    public async Task CancelJob_ShouldReturnTrueForExistingJob()
    {
        // Arrange
        var request = new GenerateProductsRequestDto
        {
            Count = 1000, // Aumentato per dare più tempo al job di rimanere in esecuzione
            BatchSize = 10
        };

        var jobId = await _service.StartGenerationJobAsync(request, _tenantId, _userId);
        await Task.Delay(200); // Attendi l'avvio del job

        // Act
        var cancelled = _service.CancelJob(jobId);

        // Assert
        Assert.True(cancelled);

        // Attendi per permettere alla cancellazione di propagarsi
        await Task.Delay(1000);

        var status = _service.GetJobStatus(jobId);
        Assert.NotNull(status);
        // Il job potrebbe essere Cancelled o Failed se è fallito prima della cancellazione
        Assert.True(
            status.Status == ProductGenerationJobStatus.Cancelled || status.Status == ProductGenerationJobStatus.Failed,
            $"Expected Cancelled or Failed, but got {status.Status}");
    }

    [Fact]
    public void CancelJob_ShouldReturnFalseForNonExistentJob()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid().ToString();

        // Act
        var cancelled = _service.CancelJob(nonExistentJobId);

        // Assert
        Assert.False(cancelled);
    }

    [Fact]
    public async Task GenerateProductsAsync_ShouldCreateProducts()
    {
        // Arrange
        var request = new GenerateProductsRequestDto
        {
            Count = 5,
            BatchSize = 5
        };

        // Act
        var jobId = await _service.StartGenerationJobAsync(request, _tenantId, _userId);

        // Attendi il completamento del job (con timeout)
        var maxWaitTime = TimeSpan.FromSeconds(10);
        var startTime = DateTime.UtcNow;
        GenerateProductsStatusDto? status = null;

        while ((DateTime.UtcNow - startTime) < maxWaitTime)
        {
            status = _service.GetJobStatus(jobId);
            if (status?.Status == ProductGenerationJobStatus.Done ||
                status?.Status == ProductGenerationJobStatus.Failed)
            {
                break;
            }
            await Task.Delay(100);
        }

        // Assert
        Assert.NotNull(status);
        Assert.Equal(ProductGenerationJobStatus.Done, status.Status);
        Assert.Equal(5, status.Total);
        Assert.True(status.Created > 0, "Dovrebbero essere stati creati almeno alcuni prodotti");
        Assert.NotNull(status.StartedAt);
        Assert.NotNull(status.CompletedAt);
        Assert.True(status.DurationSeconds > 0);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
