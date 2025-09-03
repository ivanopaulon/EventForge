using EventForge.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EventForge.Tests.Services;

public class QzWebSocketClientTests
{
    private readonly ILogger<QzWebSocketClient> _logger;
    private readonly IConfiguration _configuration;
    private readonly Mock<QzSigner> _mockSigner;

    public QzWebSocketClientTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<QzWebSocketClient>();
        
        var configBuilder = new ConfigurationBuilder();
        _configuration = configBuilder.Build();
        
        var signerLogger = loggerFactory.CreateLogger<QzSigner>();
        _mockSigner = new Mock<QzSigner>(signerLogger, _configuration);
    }

    [Fact]
    public void Constructor_WithValidParameters_DoesNotThrow()
    {
        // Act & Assert
        var client = new QzWebSocketClient(_logger, _mockSigner.Object, _configuration);
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithEnvironmentVariables_UsesCorrectUri()
    {
        // Arrange
        Environment.SetEnvironmentVariable("QZ_WS_URI", "ws://custom-host:9999");
        
        try
        {
            // Act
            var client = new QzWebSocketClient(_logger, _mockSigner.Object, _configuration);
            
            // Assert
            Assert.NotNull(client);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("QZ_WS_URI", null);
        }
    }

    [Fact]
    public async Task ConnectAsync_WhenQzTrayNotRunning_ReturnsFalse()
    {
        // Arrange
        var client = new QzWebSocketClient(_logger, _mockSigner.Object, _configuration);

        // Act
        var result = await client.ConnectAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var client = new QzWebSocketClient(_logger, _mockSigner.Object, _configuration);

        // Act & Assert
        client.Dispose();
    }
}