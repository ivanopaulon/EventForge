using EventForge.Server.Data;
using EventForge.Server.Services.CodeGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.CodeGeneration;

/// <summary>
/// Unit tests for DailySequentialCodeGenerator.
/// </summary>
[Trait("Category", "Unit")]
public class DailyCodeGeneratorTests : IDisposable
{
    private readonly EventForgeDbContext _context;
    private readonly Mock<ILogger<DailySequentialCodeGenerator>> _mockLogger;
    private readonly DailySequentialCodeGenerator _codeGenerator;

    public DailyCodeGeneratorTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<EventForgeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EventForgeDbContext(options);

        _mockLogger = new Mock<ILogger<DailySequentialCodeGenerator>>();

        _codeGenerator = new DailySequentialCodeGenerator(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateDailyCodeAsync_FirstCodeOfDay_ReturnsCorrectFormat()
    {
        // Act
        var code = await _codeGenerator.GenerateDailyCodeAsync();

        // Assert
        Assert.NotNull(code);
        Assert.Equal(14, code.Length); // YYYYMMDDNNNNNN = 8 + 6 = 14

        var dateString = DateTime.UtcNow.ToString("yyyyMMdd");
        Assert.StartsWith(dateString, code);
        Assert.EndsWith("000001", code); // First code should be 000001
    }

    [Fact]
    public async Task GenerateDailyCodeAsync_MultipleCallsSameDay_IncrementsCounter()
    {
        // Act
        var code1 = await _codeGenerator.GenerateDailyCodeAsync();
        var code2 = await _codeGenerator.GenerateDailyCodeAsync();
        var code3 = await _codeGenerator.GenerateDailyCodeAsync();

        // Assert
        var dateString = DateTime.UtcNow.ToString("yyyyMMdd");

        Assert.Equal($"{dateString}000001", code1);
        Assert.Equal($"{dateString}000002", code2);
        Assert.Equal($"{dateString}000003", code3);
    }

    [Fact]
    public async Task GenerateDailyCodeAsync_VerifySequenceInDatabase()
    {
        // Arrange
        var utcDate = DateTime.UtcNow.Date;

        // Act
        await _codeGenerator.GenerateDailyCodeAsync();
        await _codeGenerator.GenerateDailyCodeAsync();
        await _codeGenerator.GenerateDailyCodeAsync();

        // Assert
        var sequence = await _context.DailySequences
            .FirstOrDefaultAsync(ds => ds.Date == utcDate);

        Assert.NotNull(sequence);
        Assert.Equal(utcDate, sequence.Date);
        Assert.Equal(3, sequence.LastNumber);
    }

    [Fact]
    public async Task GenerateDailyCodeAsync_ZeroPadding_WorksCorrectly()
    {
        // Arrange - Generate 999 codes to test padding
        for (int i = 0; i < 9; i++)
        {
            await _codeGenerator.GenerateDailyCodeAsync();
        }

        // Act
        var code = await _codeGenerator.GenerateDailyCodeAsync();

        // Assert
        var dateString = DateTime.UtcNow.ToString("yyyyMMdd");
        Assert.Equal($"{dateString}000010", code); // Should be zero-padded to 6 digits
    }

    [Fact]
    public async Task GenerateDailyCodeAsync_CancellationToken_Respected()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _codeGenerator.GenerateDailyCodeAsync(cts.Token));
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
