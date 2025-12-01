using EventForge.DTOs.Products;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.Data.Entities.Business;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.PriceHistory;
using EventForge.Server.Services.Products;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace EventForge.Tests.Services.Products;

/// <summary>
/// Unit tests for SupplierProductCsvImportService.
/// </summary>
public class SupplierProductCsvImportServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly SupplierProductCsvImportService _service;
    private readonly Mock<ISupplierProductPriceHistoryService> _priceHistoryServiceMock;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _supplierId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public SupplierProductCsvImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EventForgeDbContext(options);
        _priceHistoryServiceMock = new Mock<ISupplierProductPriceHistoryService>();
        var logger = new Mock<ILogger<SupplierProductCsvImportService>>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CsvImport:MaxFileSizeBytes", "10485760" },
                { "CsvImport:BatchSize", "100" },
                { "CsvImport:DefaultCurrency", "EUR" },
                { "CsvImport:MaxRowsPreview", "10" }
            })
            .Build();

        _service = new SupplierProductCsvImportService(
            _context,
            _priceHistoryServiceMock.Object,
            logger.Object,
            configuration);

        // Seed initial data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = _userId,
            TenantId = _tenantId,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash"
        };

        var supplier = new BusinessParty
        {
            Id = _supplierId,
            TenantId = _tenantId,
            Name = "Test Supplier",
            PartyType = BusinessPartyType.Fornitore
        };

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Product 1",
            Code = "P001",
            Status = ProductStatus.Active
        };

        _context.Users.Add(user);
        _context.BusinessParties.Add(supplier);
        _context.Products.Add(product1);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ValidateCsvAsync_WithValidCsv_ShouldReturnSuccessfulValidation()
    {
        // Arrange
        var csvContent = $"ProductCode,ProductName,UnitCost,LeadTimeDays{Environment.NewLine}P001,Product 1,100.50,5";
        var file = CreateFormFile(csvContent, "test.csv");

        // Act
        var result = await _service.ValidateCsvAsync(_supplierId, file);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains("ProductCode", result.DetectedColumns);
        Assert.Contains("UnitCost", result.DetectedColumns);
        Assert.NotNull(result.SuggestedMapping.ProductCodeColumn);
        Assert.NotNull(result.SuggestedMapping.UnitCostColumn);
    }

    [Fact]
    public async Task ValidateCsvAsync_WithEmptyFile_ShouldReturnError()
    {
        // Arrange
        var file = CreateFormFile("", "empty.csv");

        // Act
        var result = await _service.ValidateCsvAsync(_supplierId, file);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.ErrorType == "EmptyFile");
    }

    [Fact]
    public async Task ValidateCsvAsync_WithMissingRequiredColumns_ShouldReturnError()
    {
        // Arrange
        var csvContent = $"Name,Description{Environment.NewLine}Product,Some product";
        var file = CreateFormFile(csvContent, "invalid.csv");

        // Act
        var result = await _service.ValidateCsvAsync(_supplierId, file);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.ValidationErrors, e => e.ErrorMessage.Contains("ProductCode"));
    }

    [Fact]
    public async Task ImportCsvAsync_WithValidData_ShouldCreateProductSupplierRelationship()
    {
        // Arrange
        var csvContent = $"ProductCode,ProductName,UnitCost,LeadTimeDays{Environment.NewLine}P001,Product 1,100.50,5";
        var file = CreateFormFile(csvContent, "import.csv");
        var options = new CsvImportOptions
        {
            UpdateExisting = true,
            CreateNew = false,
            ColumnMapping = new ColumnMapping
            {
                ProductCodeColumn = "ProductCode",
                ProductNameColumn = "ProductName",
                UnitCostColumn = "UnitCost",
                LeadTimeDaysColumn = "LeadTimeDays"
            }
        };

        // Act
        var result = await _service.ImportCsvAsync(_supplierId, file, options, "testuser");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalRows);
        Assert.True(result.CreatedCount > 0 || result.UpdatedCount > 0);
    }

    [Fact]
    public async Task ImportCsvAsync_WithInvalidPrice_ShouldSkipRowAndAddError()
    {
        // Arrange
        var csvContent = $"ProductCode,ProductName,UnitCost,LeadTimeDays{Environment.NewLine}P001,Product 1,invalid,5";
        var file = CreateFormFile(csvContent, "invalid_price.csv");
        var options = new CsvImportOptions
        {
            UpdateExisting = true,
            CreateNew = false,
            ColumnMapping = new ColumnMapping
            {
                ProductCodeColumn = "ProductCode",
                ProductNameColumn = "ProductName",
                UnitCostColumn = "UnitCost",
                LeadTimeDaysColumn = "LeadTimeDays"
            }
        };

        // Act
        var result = await _service.ImportCsvAsync(_supplierId, file, options, "testuser");

        // Assert
        Assert.Equal(1, result.ErrorCount);
        Assert.Contains(result.Errors, e => e.ErrorType == "ValidationError");
    }

    [Fact]
    public async Task ImportCsvAsync_WithNonExistentSupplier_ShouldReturnError()
    {
        // Arrange
        var nonExistentSupplierId = Guid.NewGuid();
        var csvContent = $"ProductCode,UnitCost{Environment.NewLine}P001,100.50";
        var file = CreateFormFile(csvContent, "test.csv");
        var options = new CsvImportOptions
        {
            ColumnMapping = new ColumnMapping
            {
                ProductCodeColumn = "ProductCode",
                UnitCostColumn = "UnitCost"
            }
        };

        // Act
        var result = await _service.ImportCsvAsync(nonExistentSupplierId, file, options, "testuser");

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ErrorType == "SupplierNotFound");
    }

    [Fact]
    public async Task ValidateCsvAsync_WithSemicolonDelimiter_ShouldDetectCorrectly()
    {
        // Arrange
        var csvContent = $"ProductCode;ProductName;UnitCost;LeadTimeDays{Environment.NewLine}P001;Product 1;100.50;5";
        var file = CreateFormFile(csvContent, "semicolon.csv");

        // Act
        var result = await _service.ValidateCsvAsync(_supplierId, file);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(";", result.FileInfo.Delimiter);
        Assert.Contains("ProductCode", result.DetectedColumns);
    }

    private IFormFile CreateFormFile(string content, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        var formFile = new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
        return formFile;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
