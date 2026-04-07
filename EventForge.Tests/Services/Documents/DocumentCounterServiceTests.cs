using EventForge.DTOs.Documents;
using EventForge.Server.Data;
using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.Services.Audit;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentCounterService focusing on automatic document numbering.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentCounterServiceTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<ILogger<DocumentCounterService>> _mockLogger;
    private readonly DocumentCounterService _documentCounterService;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _documentTypeId = Guid.NewGuid();

    public DocumentCounterServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        // Create mocks
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockLogger = new Mock<ILogger<DocumentCounterService>>();

        // Setup tenant context
        _ = _mockTenantContext.Setup(x => x.CurrentTenantId).Returns(_tenantId);

        // Create service
        _documentCounterService = new DocumentCounterService(
            _context,
            _mockAuditLogService.Object,
            _mockTenantContext.Object,
            _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var documentType = new DocumentType
        {
            Id = _documentTypeId,
            TenantId = _tenantId,
            Name = "Invoice",
            Code = "INV",
            IsStockIncrease = false,
            IsFiscal = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };

        _context.DocumentTypes.Add(documentType);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCounter()
    {
        // Arrange
        var createDto = new CreateDocumentCounterDto
        {
            DocumentTypeId = _documentTypeId,
            Series = "A",
            Year = 2024,
            PaddingLength = 5,
            ResetOnYearChange = true
        };

        // Act
        var result = await _documentCounterService.CreateAsync(createDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_documentTypeId, result.DocumentTypeId);
        Assert.Equal("A", result.Series);
        Assert.Equal(2024, result.Year);
        Assert.Equal(0, result.CurrentValue);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCounter_ThrowsException()
    {
        // Arrange
        var createDto = new CreateDocumentCounterDto
        {
            DocumentTypeId = _documentTypeId,
            Series = "B",
            Year = 2024,
            PaddingLength = 5,
            ResetOnYearChange = true
        };

        // Create first counter
        _ = await _documentCounterService.CreateAsync(createDto, "test-user");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _documentCounterService.CreateAsync(createDto, "test-user"));
    }

    [Fact]
    public async Task GenerateDocumentNumberAsync_WithNewCounter_CreatesAndIncrements()
    {
        // Arrange
        var series = "C";

        // Act
        var documentNumber = await _documentCounterService.GenerateDocumentNumberAsync(
            _documentTypeId,
            series,
            "test-user");

        // Assert
        Assert.NotNull(documentNumber);
        Assert.Contains("00001", documentNumber);
        Assert.Contains(series, documentNumber);

        // Verify counter was created and incremented
        var counter = await _context.DocumentCounters
            .FirstOrDefaultAsync(c => c.DocumentTypeId == _documentTypeId && c.Series == series);
        Assert.NotNull(counter);
        Assert.Equal(1, counter.CurrentValue);
    }

    [Fact]
    public async Task GenerateDocumentNumberAsync_WithExistingCounter_Increments()
    {
        // Arrange
        var series = "D";
        var counter = new DocumentCounter
        {
            DocumentTypeId = _documentTypeId,
            Series = series,
            CurrentValue = 5,
            Year = DateTime.UtcNow.Year,
            PaddingLength = 5,
            ResetOnYearChange = true,
            TenantId = _tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentCounters.Add(counter);
        await _context.SaveChangesAsync();

        // Act
        var documentNumber = await _documentCounterService.GenerateDocumentNumberAsync(
            _documentTypeId,
            series,
            "test-user");

        // Assert
        Assert.Contains("00006", documentNumber); // Should increment from 5 to 6

        // Verify counter was incremented
        var updatedCounter = await _context.DocumentCounters.FindAsync(counter.Id);
        Assert.Equal(6, updatedCounter!.CurrentValue);
    }

    [Fact]
    public async Task GenerateDocumentNumberAsync_WithFormatPattern_UsesPattern()
    {
        // Arrange
        var series = "E";
        var currentYear = DateTime.UtcNow.Year;
        var counter = new DocumentCounter
        {
            DocumentTypeId = _documentTypeId,
            Series = series,
            CurrentValue = 0,
            Year = currentYear,
            Prefix = "INV",
            PaddingLength = 4,
            FormatPattern = "{PREFIX}-{SERIES}/{YEAR}/{NUMBER}",
            ResetOnYearChange = true,
            TenantId = _tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentCounters.Add(counter);
        await _context.SaveChangesAsync();

        // Act
        var documentNumber = await _documentCounterService.GenerateDocumentNumberAsync(
            _documentTypeId,
            series,
            "test-user");

        // Assert
        Assert.Equal($"INV-E/{currentYear}/0001", documentNumber);
    }

    [Fact]
    public async Task GenerateDocumentNumberAsync_WithDefaultFormat_UsesDefaultPattern()
    {
        // Arrange
        var series = "F";
        var currentYear = DateTime.UtcNow.Year;
        var counter = new DocumentCounter
        {
            DocumentTypeId = _documentTypeId,
            Series = series,
            CurrentValue = 0,
            Year = currentYear,
            Prefix = "DOC",
            PaddingLength = 3,
            ResetOnYearChange = true,
            TenantId = _tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentCounters.Add(counter);
        await _context.SaveChangesAsync();

        // Act
        var documentNumber = await _documentCounterService.GenerateDocumentNumberAsync(
            _documentTypeId,
            series,
            "test-user");

        // Assert
        Assert.Equal($"DOC/F/{currentYear}/001", documentNumber);
    }

    [Fact]
    public async Task GenerateDocumentNumberAsync_ConcurrentCalls_GeneratesUniqueNumbers()
    {
        // Arrange
        var series = "G";

        // Act
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            return await _documentCounterService.GenerateDocumentNumberAsync(
                _documentTypeId,
                series,
                "test-user");
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.Equal(10, results.Distinct().Count()); // All numbers should be unique

        // Verify final counter value
        var counter = await _context.DocumentCounters
            .FirstOrDefaultAsync(c => c.DocumentTypeId == _documentTypeId && c.Series == series);
        Assert.NotNull(counter);
        Assert.Equal(10, counter.CurrentValue);
    }

    [Fact]
    public async Task GenerateDocumentNumberAsync_YearChange_ResetsCounter()
    {
        // Arrange
        var series = "H";
        var lastYear = DateTime.UtcNow.Year - 1;
        var counter = new DocumentCounter
        {
            DocumentTypeId = _documentTypeId,
            Series = series,
            CurrentValue = 100,
            Year = lastYear,
            PaddingLength = 5,
            ResetOnYearChange = true,
            TenantId = _tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentCounters.Add(counter);
        await _context.SaveChangesAsync();

        // Act
        var documentNumber = await _documentCounterService.GenerateDocumentNumberAsync(
            _documentTypeId,
            series,
            "test-user");

        // Assert
        Assert.Contains("00001", documentNumber); // Should reset to 1

        // Verify counter was reset and incremented
        // Detach to force reload
        _context.Entry(counter).State = EntityState.Detached;
        var updatedCounter = await _context.DocumentCounters
            .FirstOrDefaultAsync(c => c.DocumentTypeId == _documentTypeId && c.Series == series);
        Assert.NotNull(updatedCounter);
        Assert.Equal(1, updatedCounter.CurrentValue);
        Assert.Equal(DateTime.UtcNow.Year, updatedCounter.Year);
    }

    [Fact]
    public async Task GetByDocumentTypeAsync_ReturnsAllCountersForType()
    {
        // Arrange
        var counters = new[]
        {
            new DocumentCounter
            {
                DocumentTypeId = _documentTypeId,
                Series = "I1",
                CurrentValue = 1,
                Year = 2024,
                TenantId = _tenantId,
                CreatedBy = "test-user",
                CreatedAt = DateTime.UtcNow
            },
            new DocumentCounter
            {
                DocumentTypeId = _documentTypeId,
                Series = "I2",
                CurrentValue = 2,
                Year = 2024,
                TenantId = _tenantId,
                CreatedBy = "test-user",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.DocumentCounters.AddRange(counters);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentCounterService.GetByDocumentTypeAsync(_documentTypeId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 2);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesCounter()
    {
        // Arrange
        var counter = new DocumentCounter
        {
            DocumentTypeId = _documentTypeId,
            Series = "J",
            CurrentValue = 10,
            Year = 2024,
            PaddingLength = 5,
            TenantId = _tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentCounters.Add(counter);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateDocumentCounterDto
        {
            CurrentValue = 20,
            PaddingLength = 6,
            Prefix = "UPD"
        };

        // Act
        var result = await _documentCounterService.UpdateAsync(counter.Id, updateDto, "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.CurrentValue);
        Assert.Equal(6, result.PaddingLength);
        Assert.Equal("UPD", result.Prefix);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_SoftDeletesCounter()
    {
        // Arrange
        var counter = new DocumentCounter
        {
            DocumentTypeId = _documentTypeId,
            Series = "K",
            CurrentValue = 1,
            Year = 2024,
            TenantId = _tenantId,
            CreatedBy = "test-user",
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentCounters.Add(counter);
        await _context.SaveChangesAsync();

        // Act
        var result = await _documentCounterService.DeleteAsync(counter.Id, "test-user");

        // Assert
        Assert.True(result);

        var deletedCounter = await _context.DocumentCounters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == counter.Id);
        Assert.NotNull(deletedCounter);
        Assert.True(deletedCounter.IsDeleted);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
