using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.CodeGeneration;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for ProductService recent transactions functionality.
/// </summary>
[Trait("Category", "Unit")]
public class ProductRecentTransactionsTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly Mock<IDailyCodeGenerator> _mockCodeGenerator;
    private readonly ProductService _productService;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _purchaseDocTypeId = Guid.NewGuid();
    private readonly Guid _saleDocTypeId = Guid.NewGuid();

    public ProductRecentTransactionsTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        // Create mocks
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<ProductService>>();
        _mockCodeGenerator = new Mock<IDailyCodeGenerator>();

        // Setup tenant context
        _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create ProductService
        _productService = new ProductService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object,
            _mockCodeGenerator.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create product
        var product = new Product
        {
            Id = _productId,
            TenantId = _tenantId,
            Name = "Test Product",
            Code = "PROD001",
            Status = Server.Data.Entities.Products.ProductStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Products.Add(product);

        // Create supplier
        var supplier = new BusinessParty
        {
            Id = _supplierId,
            TenantId = _tenantId,
            Name = "Test Supplier",
            PartyType = BusinessPartyType.Fornitore,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.BusinessParties.Add(supplier);

        // Create customer
        var customer = new BusinessParty
        {
            Id = _customerId,
            TenantId = _tenantId,
            Name = "Test Customer",
            PartyType = BusinessPartyType.Cliente,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.BusinessParties.Add(customer);

        // Create purchase document type
        var purchaseDocType = new DocumentType
        {
            Id = _purchaseDocTypeId,
            TenantId = _tenantId,
            Name = "Purchase Order",
            Code = "PO",
            IsStockIncrease = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(purchaseDocType);

        // Create sale document type
        var saleDocType = new DocumentType
        {
            Id = _saleDocTypeId,
            TenantId = _tenantId,
            Name = "Sales Invoice",
            Code = "SI",
            IsStockIncrease = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentTypes.Add(saleDocType);

        // Create purchase documents with rows
        for (int i = 0; i < 3; i++)
        {
            var purchaseHeader = new DocumentHeader
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                DocumentTypeId = _purchaseDocTypeId,
                BusinessPartyId = _supplierId,
                Number = $"PO-{i + 1}",
                Date = DateTime.UtcNow.AddDays(-i * 10),
                ApprovalStatus = ApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 10),
                CreatedBy = "test"
            };
            _context.DocumentHeaders.Add(purchaseHeader);

            var purchaseRow = new DocumentRow
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                DocumentHeaderId = purchaseHeader.Id,
                ProductId = _productId,
                Description = "Test Product",
                Quantity = 10 + i,
                UnitPrice = 100 - (i * 10),
                BaseUnitPrice = 100 - (i * 10),
                LineDiscount = 10, // 10% discount
                DiscountType = EventForge.DTOs.Common.DiscountType.Percentage,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 10),
                CreatedBy = "test"
            };
            _context.DocumentRows.Add(purchaseRow);
        }

        // Create sale documents with rows
        for (int i = 0; i < 2; i++)
        {
            var saleHeader = new DocumentHeader
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                DocumentTypeId = _saleDocTypeId,
                BusinessPartyId = _customerId,
                Number = $"SI-{i + 1}",
                Date = DateTime.UtcNow.AddDays(-i * 15),
                ApprovalStatus = ApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 15),
                CreatedBy = "test"
            };
            _context.DocumentHeaders.Add(saleHeader);

            var saleRow = new DocumentRow
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                DocumentHeaderId = saleHeader.Id,
                ProductId = _productId,
                Description = "Test Product",
                Quantity = 5 + i,
                UnitPrice = 150 + (i * 20),
                BaseUnitPrice = 150 + (i * 20),
                LineDiscount = 5, // 5% discount
                DiscountType = EventForge.DTOs.Common.DiscountType.Percentage,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 15),
                CreatedBy = "test"
            };
            _context.DocumentRows.Add(saleRow);
        }

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetRecentProductTransactions_Purchase_ReturnsTopN()
    {
        // Act
        var result = await _productService.GetRecentProductTransactionsAsync(
            _productId,
            type: "purchase",
            partyId: null,
            top: 3);

        var transactions = result.ToList();

        // Assert
        Assert.NotNull(transactions);
        Assert.Equal(3, transactions.Count);
        
        // Verify they are ordered by date descending (most recent first)
        Assert.True(transactions[0].DocumentDate >= transactions[1].DocumentDate);
        Assert.True(transactions[1].DocumentDate >= transactions[2].DocumentDate);
        
        // Verify effective unit price calculation (100 - 10% = 90, 90 - 10% = 81, 80 - 10% = 72)
        Assert.Equal(90m, transactions[0].EffectiveUnitPrice);
        Assert.Equal(81m, transactions[1].EffectiveUnitPrice);
        Assert.Equal(72m, transactions[2].EffectiveUnitPrice);
    }

    [Fact]
    public async Task GetRecentProductTransactions_Sale_ReturnsTopN()
    {
        // Act
        var result = await _productService.GetRecentProductTransactionsAsync(
            _productId,
            type: "sale",
            partyId: null,
            top: 3);

        var transactions = result.ToList();

        // Assert
        Assert.NotNull(transactions);
        Assert.Equal(2, transactions.Count); // Only 2 sales exist
        
        // Verify effective unit price calculation (150 - 5% = 142.5, 170 - 5% = 161.5)
        Assert.Equal(142.5m, transactions[0].EffectiveUnitPrice);
        Assert.Equal(161.5m, transactions[1].EffectiveUnitPrice);
    }

    [Fact]
    public async Task GetRecentProductTransactions_WithPartyFilter_ReturnsFilteredResults()
    {
        // Act
        var result = await _productService.GetRecentProductTransactionsAsync(
            _productId,
            type: "purchase",
            partyId: _supplierId,
            top: 3);

        var transactions = result.ToList();

        // Assert
        Assert.NotNull(transactions);
        Assert.Equal(3, transactions.Count);
        Assert.All(transactions, t => Assert.Equal(_supplierId, t.PartyId));
        Assert.All(transactions, t => Assert.Equal("Test Supplier", t.PartyName));
    }

    [Fact]
    public async Task GetRecentProductTransactions_NonExistentProduct_ReturnsEmpty()
    {
        // Act
        var result = await _productService.GetRecentProductTransactionsAsync(
            Guid.NewGuid(),
            type: "purchase",
            partyId: null,
            top: 3);

        var transactions = result.ToList();

        // Assert
        Assert.NotNull(transactions);
        Assert.Empty(transactions);
    }

    [Fact]
    public async Task GetRecentProductTransactions_ValueDiscount_CalculatesCorrectly()
    {
        // Arrange - Create a document with value discount
        var valueDiscountHeader = new DocumentHeader
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentTypeId = _purchaseDocTypeId,
            BusinessPartyId = _supplierId,
            Number = "PO-VALUE",
            Date = DateTime.UtcNow,
            ApprovalStatus = ApprovalStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentHeaders.Add(valueDiscountHeader);

        var valueDiscountRow = new DocumentRow
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            DocumentHeaderId = valueDiscountHeader.Id,
            ProductId = _productId,
            Description = "Test Product",
            Quantity = 10,
            UnitPrice = 100,
            BaseUnitPrice = 100,
            LineDiscountValue = 50, // 50 total discount for 10 units = 5 per unit
            DiscountType = EventForge.DTOs.Common.DiscountType.Value,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.DocumentRows.Add(valueDiscountRow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _productService.GetRecentProductTransactionsAsync(
            _productId,
            type: "purchase",
            partyId: null,
            top: 1);

        var transactions = result.ToList();

        // Assert
        Assert.NotNull(transactions);
        Assert.Single(transactions);
        
        // Verify effective unit price calculation (100 - (50/10) = 95)
        Assert.Equal(95m, transactions[0].EffectiveUnitPrice);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
