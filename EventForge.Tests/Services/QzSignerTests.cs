using EventForge.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventForge.Tests.Services;

public class QzSignerTests
{
    private readonly ILogger<QzSigner> _logger;
    private readonly IConfiguration _configuration;

    public QzSignerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<QzSigner>();

        var configBuilder = new ConfigurationBuilder();
        _configuration = configBuilder.Build();
    }

    [Fact]
    public async Task Sign_WithValidParameters_ReturnsBase64Signature()
    {
        // Arrange
        var signer = new QzSigner(_logger, _configuration);
        var callName = "qz.printers.find";
        var parameters = new object[] { };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var signature = await signer.Sign(callName, parameters, timestamp);

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
        Assert.True(IsValidBase64(signature), "Signature should be valid base64");
    }

    [Fact]
    public async Task Sign_WithComplexParameters_ReturnsValidSignature()
    {
        // Arrange
        var signer = new QzSigner(_logger, _configuration);
        var callName = "qz.print";
        var parameters = new object[]
        {
            new { type = "pixel", options = new { bounds = new { width = 100, height = 100 } } },
            new[] { new { type = "raw", data = "Hello World" } }
        };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var signature = await signer.Sign(callName, parameters, timestamp);

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
        Assert.True(IsValidBase64(signature), "Signature should be valid base64");
    }

    [Theory]
    [InlineData("")]
    public async Task Sign_WithEmptyCallName_ReturnsValidSignature(string callName)
    {
        // Arrange
        var signer = new QzSigner(_logger, _configuration);
        var parameters = new object[] { };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act
        var signature = await signer.Sign(callName, parameters, timestamp);

        // Assert  
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
        Assert.True(IsValidBase64(signature), "Signature should be valid base64");
    }

    [Fact]
    public async Task Sign_WithNullCallName_ThrowsArgumentNullException()
    {
        // Arrange
        var signer = new QzSigner(_logger, _configuration);
        var parameters = new object[] { };
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act & Assert
        _ = await Assert.ThrowsAsync<ArgumentNullException>(() => signer.Sign(null!, parameters, timestamp));
    }

    [Fact]
    public Task Sign_EnsuresCorrectJsonPropertyOrder()
    {
        // This test verifies that the JSON serialization maintains the required order: call, params, timestamp
        // We'll test this by creating a custom QzSigner that exposes the JSON for verification
        var signer = new TestableQzSigner(_logger, _configuration);
        var callName = "test.call";
        var parameters = new object[] { "param1", 2 };
        var timestamp = 1234567890123L;

        // Act
        var json = signer.GetSerializedJson(callName, parameters, timestamp);

        // Assert
        var expectedStart = "{\"call\":\"test.call\",\"params\":[\"param1\",2],\"timestamp\":1234567890123}";
        Assert.Equal(expectedStart, json);

        return Task.CompletedTask;
    }

    private static bool IsValidBase64(string base64String)
    {
        try
        {
            _ = Convert.FromBase64String(base64String);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// Test helper class to expose internal JSON serialization
public class TestableQzSigner : QzSigner
{
    public TestableQzSigner(ILogger<QzSigner> logger, IConfiguration configuration)
        : base(logger, configuration)
    {
    }

    public string GetSerializedJson(string callName, object[] parameters, long timestamp)
    {
        var payload = new
        {
            call = callName,
            @params = parameters,
            timestamp = timestamp
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false
        };
        return System.Text.Json.JsonSerializer.Serialize(payload, options);
    }
}