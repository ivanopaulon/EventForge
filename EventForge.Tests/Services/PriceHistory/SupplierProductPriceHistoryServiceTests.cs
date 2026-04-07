using EventForge.DTOs.PriceHistory;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.PriceHistory;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.PriceHistory;

/// <summary>
/// Unit tests for SupplierProductPriceHistoryService.
/// </summary>
[Trait("Category", "Unit")]
public class SupplierProductPriceHistoryServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<SupplierProductPriceHistoryService>> _mockLogger;
    private readonly SupplierProductPriceHistoryService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _productSupplierId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public SupplierProductPriceHistoryServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        // Create mocks
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<SupplierProductPriceHistoryService>>();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);
        _ = _mockTenantContext.Setup(x => x.CurrentUserId).Returns(_userId);

        // Create service
        _service = new SupplierProductPriceHistoryService(
            _context,
            _mockTenantContext.Object,
            _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create user
        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);

        // Create supplier
        var supplier = new BusinessParty
        {
            Id = _supplierId,
            TenantId = _tenantId,
            Name = "Test Supplier",
            PartyType = BusinessPartyType.Fornitore,
            CreatedAt = DateTime.UtcNow
        };
        _context.BusinessParties.Add(supplier);

        // Create product
        var product = new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Name = "Test Product",
            Code = "PROD001",
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _context.Products.Add(product);

        // Create product supplier
        var productSupplier = new ProductSupplier
        {
            Id = _productSupplierId,
            TenantId = _tenantId,
            ProductId = _productId,
            SupplierId = _supplierId,
            UnitCost = 100.00m,
            Currency = "EUR",
            LeadTimeDays = 7,
            CreatedAt = DateTime.UtcNow
        };
        _context.ProductSuppliers.Add(productSupplier);

        _context.SaveChanges();
    }

    [Fact]
    public async Task LogPriceChangeAsync_ShouldCreatePriceHistoryEntry()
    {
        // Arrange
        var request = new PriceChangeLogRequest
        {
            ProductSupplierId = _productSupplierId,
            SupplierId = _supplierId,
            ProductId = _productId,
            OldPrice = 100.00m,
            NewPrice = 120.00m,
            Currency = "EUR",
            OldLeadTimeDays = 7,
            NewLeadTimeDays = 5,
            ChangeSource = "Manual",
            ChangeReason = "Supplier price increase",
            UserId = _userId
        };

        // Act
        var historyId = await _service.LogPriceChangeAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, historyId);

        var history = await _context.SupplierProductPriceHistories
            .FirstOrDefaultAsync(h => h.Id == historyId);

        Assert.NotNull(history);
        Assert.Equal(_productSupplierId, history.ProductSupplierId);
        Assert.Equal(_supplierId, history.SupplierId);
        Assert.Equal(_productId, history.ProductId);
        Assert.Equal(100.00m, history.OldUnitCost);
        Assert.Equal(120.00m, history.NewUnitCost);
        Assert.Equal(20.00m, history.PriceChange);
        Assert.Equal(20.0m, history.PriceChangePercentage);
        Assert.Equal("EUR", history.Currency);
        Assert.Equal(7, history.OldLeadTimeDays);
        Assert.Equal(5, history.NewLeadTimeDays);
        Assert.Equal("Manual", history.ChangeSource);
        Assert.Equal("Supplier price increase", history.ChangeReason);
        Assert.Equal(_userId, history.ChangedByUserId);
    }

    [Fact]
    public async Task LogPriceChangeAsync_ShouldCalculatePercentageCorrectly_ForPriceIncrease()
    {
        // Arrange
        var request = new PriceChangeLogRequest
        {
            ProductSupplierId = _productSupplierId,
            SupplierId = _supplierId,
            ProductId = _productId,
            OldPrice = 100.00m,
            NewPrice = 150.00m,
            Currency = "EUR",
            ChangeSource = "Manual",
            UserId = _userId
        };

        // Act
        var historyId = await _service.LogPriceChangeAsync(request);

        // Assert
        var history = await _context.SupplierProductPriceHistories
            .FirstOrDefaultAsync(h => h.Id == historyId);

        Assert.NotNull(history);
        Assert.Equal(50.00m, history.PriceChange);
        Assert.Equal(50.0m, history.PriceChangePercentage);
    }

    [Fact]
    public async Task LogPriceChangeAsync_ShouldCalculatePercentageCorrectly_ForPriceDecrease()
    {
        // Arrange
        var request = new PriceChangeLogRequest
        {
            ProductSupplierId = _productSupplierId,
            SupplierId = _supplierId,
            ProductId = _productId,
            OldPrice = 100.00m,
            NewPrice = 75.00m,
            Currency = "EUR",
            ChangeSource = "BulkEdit",
            UserId = _userId
        };

        // Act
        var historyId = await _service.LogPriceChangeAsync(request);

        // Assert
        var history = await _context.SupplierProductPriceHistories
            .FirstOrDefaultAsync(h => h.Id == historyId);

        Assert.NotNull(history);
        Assert.Equal(-25.00m, history.PriceChange);
        Assert.Equal(-25.0m, history.PriceChangePercentage);
    }

    [Fact]
    public async Task GetProductPriceHistoryAsync_ShouldReturnFilteredResults()
    {
        // Arrange - Create some price history entries
        var history1 = new SupplierProductPriceHistory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductSupplierId = _productSupplierId,
            SupplierId = _supplierId,
            ProductId = _productId,
            OldUnitCost = 100,
            NewUnitCost = 110,
            PriceChange = 10,
            PriceChangePercentage = 10,
            Currency = "EUR",
            ChangedAt = DateTime.UtcNow.AddDays(-5),
            ChangedByUserId = _userId,
            ChangeSource = "Manual",
            CreatedAt = DateTime.UtcNow
        };

        var history2 = new SupplierProductPriceHistory
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductSupplierId = _productSupplierId,
            SupplierId = _supplierId,
            ProductId = _productId,
            OldUnitCost = 110,
            NewUnitCost = 120,
            PriceChange = 10,
            PriceChangePercentage = 9.09m,
            Currency = "EUR",
            ChangedAt = DateTime.UtcNow.AddDays(-2),
            ChangedByUserId = _userId,
            ChangeSource = "BulkEdit",
            CreatedAt = DateTime.UtcNow
        };

        _context.SupplierProductPriceHistories.AddRange(history1, history2);
        await _context.SaveChangesAsync();

        var request = new PriceHistoryRequest
        {
            Page = 1,
            PageSize = 10,
            SortBy = "ChangedAt",
            SortDirection = "Desc"
        };

        // Act
        var result = await _service.GetProductPriceHistoryAsync(_supplierId, _productId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.TotalPages);
        Assert.True(result.Items[0].ChangedAt > result.Items[1].ChangedAt); // Descending order
    }

    [Fact]
    public async Task GetPriceHistoryStatisticsAsync_ShouldCalculateCorrectly()
    {
        // Arrange - Create price history entries
        var histories = new[]
        {
            new SupplierProductPriceHistory
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ProductSupplierId = _productSupplierId,
                SupplierId = _supplierId,
                ProductId = _productId,
                OldUnitCost = 100,
                NewUnitCost = 120,
                PriceChange = 20,
                PriceChangePercentage = 20,
                Currency = "EUR",
                ChangedAt = DateTime.UtcNow.AddDays(-5),
                ChangedByUserId = _userId,
                ChangeSource = "Manual",
                CreatedAt = DateTime.UtcNow
            },
            new SupplierProductPriceHistory
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                ProductSupplierId = _productSupplierId,
                SupplierId = _supplierId,
                ProductId = _productId,
                OldUnitCost = 120,
                NewUnitCost = 100,
                PriceChange = -20,
                PriceChangePercentage = -16.67m,
                Currency = "EUR",
                ChangedAt = DateTime.UtcNow.AddDays(-2),
                ChangedByUserId = _userId,
                ChangeSource = "Manual",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.SupplierProductPriceHistories.AddRange(histories);
        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetPriceHistoryStatisticsAsync(_supplierId, _productId);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(2, stats.TotalChanges);
        Assert.Equal(1, stats.TotalIncreases);
        Assert.Equal(1, stats.TotalDecreases);
        Assert.Equal(20, stats.MaxPriceIncrease);
        Assert.Equal(-16.67m, stats.MaxPriceDecrease);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
