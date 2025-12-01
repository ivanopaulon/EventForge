using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.PriceHistory;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for SupplierSuggestionService.
/// </summary>
public class SupplierSuggestionServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ISupplierProductPriceHistoryService> _priceHistoryServiceMock;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly SupplierSuggestionService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _supplier1Id = Guid.NewGuid();
    private readonly Guid _supplier2Id = Guid.NewGuid();
    private readonly Guid _supplier3Id = Guid.NewGuid();

    public SupplierSuggestionServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);

        _tenantContextMock = new Mock<ITenantContext>();
        _tenantContextMock.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        _priceHistoryServiceMock = new Mock<ISupplierProductPriceHistoryService>();

        _cache = new MemoryCache(new MemoryCacheOptions());

        var configDict = new Dictionary<string, string?>
        {
            { "SupplierSuggestion:Weights:Price", "0.4" },
            { "SupplierSuggestion:Weights:LeadTime", "0.25" },
            { "SupplierSuggestion:Weights:Reliability", "0.2" },
            { "SupplierSuggestion:Weights:Trend", "0.15" },
            { "SupplierSuggestion:MinDataPointsForTrend", "3" },
            { "SupplierSuggestion:TrendAnalysisPeriodDays", "180" },
            { "SupplierSuggestion:CacheScoresDurationMinutes", "5" },
            { "SupplierSuggestion:ConfidenceThresholds:Low", "60" },
            { "SupplierSuggestion:ConfidenceThresholds:High", "80" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var logger = new Mock<ILogger<SupplierSuggestionService>>();

        _service = new SupplierSuggestionService(
            _context,
            _tenantContextMock.Object,
            _priceHistoryServiceMock.Object,
            _cache,
            _configuration,
            logger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var product = new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Name = "Test Product",
            Code = "TEST001"
        };

        var supplier1 = new BusinessParty
        {
            Id = _supplier1Id,
            TenantId = _tenantId,
            Name = "Supplier A",
            PartyType = BusinessPartyType.Fornitore
        };

        var supplier2 = new BusinessParty
        {
            Id = _supplier2Id,
            TenantId = _tenantId,
            Name = "Supplier B",
            PartyType = BusinessPartyType.Fornitore
        };

        var supplier3 = new BusinessParty
        {
            Id = _supplier3Id,
            TenantId = _tenantId,
            Name = "Supplier C",
            PartyType = BusinessPartyType.Fornitore
        };

        _context.Products.Add(product);
        _context.BusinessParties.AddRange(supplier1, supplier2, supplier3);

        // Add product-supplier relationships
        _context.ProductSuppliers.AddRange(
            new ProductSupplier
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ProductId = _productId,
                SupplierId = _supplier1Id,
                UnitCost = 10.00m,
                Currency = "EUR",
                LeadTimeDays = 5,
                Preferred = true
            },
            new ProductSupplier
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ProductId = _productId,
                SupplierId = _supplier2Id,
                UnitCost = 8.50m, // Lower price
                Currency = "EUR",
                LeadTimeDays = 7,
                Preferred = false
            },
            new ProductSupplier
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ProductId = _productId,
                SupplierId = _supplier3Id,
                UnitCost = 12.00m, // Higher price
                Currency = "EUR",
                LeadTimeDays = 3, // Shortest lead time
                Preferred = false
            }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetSupplierSuggestionsAsync_ReturnsValidResponse()
    {
        // Act
        var result = await _service.GetSupplierSuggestionsAsync(_productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_productId, result.ProductId);
        Assert.Equal("TEST001", result.ProductCode);
        Assert.Equal("Test Product", result.ProductName);
        Assert.Equal(3, result.Suggestions.Count);
        Assert.NotNull(result.RecommendedSupplier);
        Assert.NotNull(result.RecommendationExplanation);
    }

    [Fact]
    public async Task CalculateSuggestionsAsync_ReturnsSuggestionsOrderedByScore()
    {
        // Act
        var result = await _service.CalculateSuggestionsAsync(_productId);

        // Assert
        Assert.Equal(3, result.Count);

        // Verify descending order by total score
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].TotalScore >= result[i + 1].TotalScore);
        }
    }

    [Fact]
    public async Task CalculateSuggestionsAsync_CalculatesPriceScoreCorrectly()
    {
        // Act
        var result = await _service.CalculateSuggestionsAsync(_productId);

        // Assert
        var cheapestSupplier = result.FirstOrDefault(s => s.UnitCost == 8.50m);
        var mostExpensiveSupplier = result.FirstOrDefault(s => s.UnitCost == 12.00m);

        Assert.NotNull(cheapestSupplier);
        Assert.NotNull(mostExpensiveSupplier);

        // Cheapest should have highest price score
        Assert.True(cheapestSupplier.ScoreBreakdown.PriceScore > mostExpensiveSupplier.ScoreBreakdown.PriceScore);

        // Cheapest should have 100 price score (best price)
        Assert.Equal(100m, cheapestSupplier.ScoreBreakdown.PriceScore);

        // Most expensive should have 0 price score (worst price)
        Assert.Equal(0m, mostExpensiveSupplier.ScoreBreakdown.PriceScore);
    }

    [Fact]
    public async Task CalculateSuggestionsAsync_CalculatesLeadTimeScoreCorrectly()
    {
        // Act
        var result = await _service.CalculateSuggestionsAsync(_productId);

        // Assert
        var fastestSupplier = result.FirstOrDefault(s => s.LeadTimeDays == 3);
        var slowestSupplier = result.FirstOrDefault(s => s.LeadTimeDays == 7);

        Assert.NotNull(fastestSupplier);
        Assert.NotNull(slowestSupplier);

        // Fastest should have highest lead time score
        Assert.True(fastestSupplier.ScoreBreakdown.LeadTimeScore > slowestSupplier.ScoreBreakdown.LeadTimeScore);

        // Fastest should have 100 lead time score
        Assert.Equal(100m, fastestSupplier.ScoreBreakdown.LeadTimeScore);

        // Slowest should have 0 lead time score
        Assert.Equal(0m, slowestSupplier.ScoreBreakdown.LeadTimeScore);
    }

    [Fact]
    public async Task CalculateSuggestionsAsync_CalculatesTotalScoreWithWeights()
    {
        // Act
        var result = await _service.CalculateSuggestionsAsync(_productId);

        // Assert
        foreach (var suggestion in result)
        {
            var expectedTotal =
                (suggestion.ScoreBreakdown.PriceScore * 0.4m) +
                (suggestion.ScoreBreakdown.LeadTimeScore * 0.25m) +
                (suggestion.ScoreBreakdown.ReliabilityScore * 0.2m) +
                (suggestion.ScoreBreakdown.TrendScore * 0.15m);

            Assert.Equal(expectedTotal, suggestion.TotalScore, 2);
        }
    }

    [Fact]
    public async Task CalculateSuggestionsAsync_SetsConfidenceLevelCorrectly()
    {
        // Act
        var result = await _service.CalculateSuggestionsAsync(_productId);

        // Assert
        foreach (var suggestion in result)
        {
            if (suggestion.TotalScore < 60)
            {
                Assert.Equal(EventForge.DTOs.Products.SupplierSuggestion.ConfidenceLevel.Low, suggestion.Confidence);
            }
            else if (suggestion.TotalScore < 80)
            {
                Assert.Equal(EventForge.DTOs.Products.SupplierSuggestion.ConfidenceLevel.Medium, suggestion.Confidence);
            }
            else
            {
                Assert.Equal(EventForge.DTOs.Products.SupplierSuggestion.ConfidenceLevel.High, suggestion.Confidence);
            }
        }
    }

    [Fact]
    public async Task CalculateSuggestionsAsync_MarksCurrentPreferredSupplier()
    {
        // Act
        var result = await _service.CalculateSuggestionsAsync(_productId);

        // Assert
        var preferredSuppliers = result.Where(s => s.IsCurrentPreferred).ToList();
        Assert.Single(preferredSuppliers);
        Assert.Equal(_supplier1Id, preferredSuppliers[0].SupplierId);
    }

    [Fact]
    public async Task CalculateSuggestionsAsync_AddsExplanationsToScores()
    {
        // Act
        var result = await _service.CalculateSuggestionsAsync(_productId);

        // Assert
        foreach (var suggestion in result)
        {
            Assert.NotEmpty(suggestion.ScoreBreakdown.Explanations);
            Assert.True(suggestion.ScoreBreakdown.Explanations.ContainsKey("Price"));
            Assert.True(suggestion.ScoreBreakdown.Explanations.ContainsKey("LeadTime"));
            Assert.True(suggestion.ScoreBreakdown.Explanations.ContainsKey("Reliability"));
            Assert.True(suggestion.ScoreBreakdown.Explanations.ContainsKey("Trend"));
        }
    }

    [Fact]
    public async Task ApplySuggestedSupplierAsync_UpdatesPreferredSupplier()
    {
        // Arrange
        var newPreferredSupplierId = _supplier2Id;

        // Act
        var result = await _service.ApplySuggestedSupplierAsync(_productId, newPreferredSupplierId, "Better price");

        // Assert
        Assert.True(result);

        var productSuppliers = await _context.ProductSuppliers
            .Where(ps => ps.ProductId == _productId && ps.TenantId == _tenantId)
            .ToListAsync();

        var newPreferred = productSuppliers.First(ps => ps.SupplierId == newPreferredSupplierId);
        Assert.True(newPreferred.Preferred);

        var oldPreferred = productSuppliers.First(ps => ps.SupplierId == _supplier1Id);
        Assert.False(oldPreferred.Preferred);
    }

    [Fact]
    public async Task ApplySuggestedSupplierAsync_AddsReasonToNotes()
    {
        // Arrange
        var newPreferredSupplierId = _supplier2Id;
        var reason = "Better price and faster delivery";

        // Act
        await _service.ApplySuggestedSupplierAsync(_productId, newPreferredSupplierId, reason);

        // Assert
        var productSupplier = await _context.ProductSuppliers
            .FirstAsync(ps => ps.ProductId == _productId && ps.SupplierId == newPreferredSupplierId);

        Assert.Contains("Applied suggestion", productSupplier.Notes);
        Assert.Contains(reason, productSupplier.Notes);
    }

    [Fact]
    public async Task GetSupplierReliabilityAsync_ReturnsReliabilityMetrics()
    {
        // Act
        var result = await _service.GetSupplierReliabilityAsync(_supplier1Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_supplier1Id, result.SupplierId);
        Assert.Equal("Supplier A", result.SupplierName);
        Assert.True(result.OverallReliabilityScore > 0);
        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.OnTimeDeliveryRate >= 0);
        Assert.True(result.Metrics.OrderAccuracyRate >= 0);
    }

    [Fact]
    public async Task GetSupplierSuggestionsAsync_CalculatesPotentialSavings()
    {
        // Act
        var result = await _service.GetSupplierSuggestionsAsync(_productId);

        // Assert
        // Current preferred is Supplier A at 10.00
        // If recommended is Supplier B at 8.50, savings should be 1.50
        if (result.RecommendedSupplier?.UnitCost == 8.50m)
        {
            Assert.Equal(1.50m, result.PotentialSavings);
        }
    }

    [Fact]
    public async Task CalculateSuggestionsAsync_ReturnsEmptyListForSingleSupplier()
    {
        // Arrange - Create a new product with only one supplier
        var singleSupplierProductId = Guid.NewGuid();
        var singleSupplierProduct = new Product
        {
            Id = singleSupplierProductId,
            TenantId = _tenantId,
            Name = "Single Supplier Product",
            Code = "SINGLE001"
        };
        _context.Products.Add(singleSupplierProduct);
        _context.ProductSuppliers.Add(new ProductSupplier
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = singleSupplierProductId,
            SupplierId = _supplier1Id,
            UnitCost = 10.00m
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CalculateSuggestionsAsync(singleSupplierProductId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GenerateRecommendationExplanationAsync_CreatesNaturalLanguageText()
    {
        // Arrange
        var suggestions = await _service.CalculateSuggestionsAsync(_productId);
        var topSuggestion = suggestions.OrderByDescending(s => s.TotalScore).First();
        var product = await _context.Products.FirstAsync(p => p.Id == _productId);

        // Act
        var explanation = await _service.GenerateRecommendationExplanationAsync(topSuggestion, product);

        // Assert
        Assert.NotNull(explanation);
        Assert.NotEmpty(explanation);
        Assert.Contains(topSuggestion.SupplierName!, explanation);
        Assert.Contains(product.Name, explanation);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }
}
