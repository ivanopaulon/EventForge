using Prym.DTOs.Health;
using Prym.Server.Controllers;
using Prym.Server.Data;
using Prym.Server.Services.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Prym.Tests;

[Trait("Category", "Unit")]
public class HealthControllerTests
{
    [Fact]
    public void HealthController_Constructor_ShouldNotThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<PrymDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        var context = new PrymDbContext(options);
        var logger = new LoggerFactory().CreateLogger<HealthController>();
        var mockFirstRunService = new Mock<IFirstRunDetectionService>();

        // Act & Assert
        var controller = new HealthController(context, logger, mockFirstRunService.Object);
        Assert.NotNull(controller);
    }

    [Fact]
    public void HealthStatusDto_Creation_ShouldSucceed()
    {
        // Arrange & Act
        var healthStatus = new HealthStatusDto
        {
            ApiStatus = "Healthy",
            DatabaseStatus = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        };

        // Assert
        Assert.NotNull(healthStatus);
        Assert.Equal("Healthy", healthStatus.ApiStatus);
        Assert.Equal("Healthy", healthStatus.DatabaseStatus);
    }
}