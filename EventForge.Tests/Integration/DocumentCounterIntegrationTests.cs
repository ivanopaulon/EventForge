using EventForge.DTOs.Documents;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Tests.Integration;

/// <summary>
/// Integration tests for DocumentCounter functionality including automatic document numbering.
/// </summary>
[Trait("Category", "Integration")]
public class DocumentCounterIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly EventForgeDbContext _context;

    public DocumentCounterIntegrationTests()
    {
        // Create in-memory database
        var services = new ServiceCollection();
        services.AddDbContext<EventForgeDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<EventForgeDbContext>();
    }

    [Fact]
    public async Task CreateDocumentHeader_WithoutNumber_AutoGeneratesNumber()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();

        // Create required entities
        var documentType = new DocumentType
        {
            Id = documentTypeId,
            TenantId = tenantId,
            Name = "Invoice",
            Code = "INV",
            IsStockIncrease = false,
            IsFiscal = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        _context.DocumentTypes.Add(documentType);
        await _context.SaveChangesAsync();

        // Create document counter
        var counter = new DocumentCounter
        {
            DocumentTypeId = documentTypeId,
            Series = "A",
            CurrentValue = 0,
            Year = DateTime.UtcNow.Year,
            PaddingLength = 5,
            ResetOnYearChange = true,
            TenantId = tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        _context.DocumentCounters.Add(counter);
        await _context.SaveChangesAsync();

        // Act - Increment and generate number
        counter.CurrentValue++;
        await _context.SaveChangesAsync();

        // Format number
        var number = $"{counter.Series}/{counter.Year}/{counter.CurrentValue.ToString().PadLeft(counter.PaddingLength, '0')}";

        // Assert
        Assert.NotNull(number);
        Assert.NotEmpty(number);
        Assert.Contains("A", number);
        Assert.Contains("00001", number);
        Assert.Equal(1, counter.CurrentValue);
    }

    [Fact]
    public async Task MultipleDocuments_SameSeries_GeneratesSequentialNumbers()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();

        // Create required entities
        var documentType = new DocumentType
        {
            Id = documentTypeId,
            TenantId = tenantId,
            Name = "Invoice",
            Code = "INV",
            IsStockIncrease = false,
            IsFiscal = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        _context.DocumentTypes.Add(documentType);

        var counter = new DocumentCounter
        {
            DocumentTypeId = documentTypeId,
            Series = "B",
            CurrentValue = 0,
            Year = DateTime.UtcNow.Year,
            PaddingLength = 5,
            ResetOnYearChange = true,
            TenantId = tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        _context.DocumentCounters.Add(counter);
        await _context.SaveChangesAsync();

        // Act - Create 5 documents
        var documentNumbers = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            counter.CurrentValue++;
            await _context.SaveChangesAsync();

            var number = $"{counter.Series}/{counter.Year}/{counter.CurrentValue.ToString().PadLeft(counter.PaddingLength, '0')}";
            documentNumbers.Add(number);
        }

        // Assert
        Assert.Equal(5, documentNumbers.Count);
        Assert.Equal(5, counter.CurrentValue);
        Assert.All(documentNumbers, n => Assert.Contains("B", n));
        Assert.Contains("00001", documentNumbers[0]);
        Assert.Contains("00005", documentNumbers[4]);
    }

    [Fact]
    public async Task DifferentSeries_MaintainSeparateCounters()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var documentTypeId = Guid.NewGuid();

        var documentType = new DocumentType
        {
            Id = documentTypeId,
            TenantId = tenantId,
            Name = "Invoice",
            Code = "INV",
            IsStockIncrease = false,
            IsFiscal = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        _context.DocumentTypes.Add(documentType);

        var counterA = new DocumentCounter
        {
            DocumentTypeId = documentTypeId,
            Series = "C",
            CurrentValue = 0,
            Year = DateTime.UtcNow.Year,
            PaddingLength = 5,
            ResetOnYearChange = true,
            TenantId = tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        var counterB = new DocumentCounter
        {
            DocumentTypeId = documentTypeId,
            Series = "D",
            CurrentValue = 0,
            Year = DateTime.UtcNow.Year,
            PaddingLength = 5,
            ResetOnYearChange = true,
            TenantId = tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };

        _context.DocumentCounters.AddRange(counterA, counterB);
        await _context.SaveChangesAsync();

        // Act - Increment both counters
        counterA.CurrentValue++;
        counterB.CurrentValue++;
        await _context.SaveChangesAsync();

        // Assert
        var counterAFromDb = await _context.DocumentCounters
            .FirstOrDefaultAsync(c => c.Series == "C");
        var counterBFromDb = await _context.DocumentCounters
            .FirstOrDefaultAsync(c => c.Series == "D");

        Assert.NotNull(counterAFromDb);
        Assert.NotNull(counterBFromDb);
        Assert.Equal(1, counterAFromDb.CurrentValue);
        Assert.Equal(1, counterBFromDb.CurrentValue);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
