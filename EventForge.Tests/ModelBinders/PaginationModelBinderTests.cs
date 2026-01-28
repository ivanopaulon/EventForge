using EventForge.DTOs.Common;
using EventForge.Server.Configuration;
using EventForge.Server.ModelBinders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace EventForge.Tests.ModelBinders;

[Trait("Category", "Unit")]
public class PaginationModelBinderTests
{
    private readonly Mock<ILogger<PaginationModelBinder>> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly PaginationSettings _settings;

    public PaginationModelBinderTests()
    {
        _loggerMock = new Mock<ILogger<PaginationModelBinder>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        _settings = new PaginationSettings
        {
            DefaultPageSize = 20,
            MaxPageSize = 1000,
            MaxExportPageSize = 10000,
            RecommendedPageSize = 100,
            EndpointOverrides = new Dictionary<string, int>
            {
                { "/api/v1/stock/overview", 5000 },
                { "/api/v1/export/*", 10000 }
            },
            RoleBasedLimits = new Dictionary<string, int>
            {
                { "User", 1000 },
                { "Admin", 5000 },
                { "SuperAdmin", 10000 }
            }
        };
    }

    [Theory]
    [InlineData(1, 50, 1, 50)]
    [InlineData(2, 100, 2, 100)]
    [InlineData(10, 20, 10, 20)]
    public async Task BindModelAsync_WithValidParameters_ReturnsExpectedValues(
        int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        // Arrange
        var (binder, bindingContext) = CreateBinder(page.ToString(), pageSize.ToString());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedPageSize, result.PageSize);
        Assert.False(result.WasCapped);
    }

    [Fact]
    public async Task BindModelAsync_WithMissingParameters_UsesDefaults()
    {
        // Arrange
        var (binder, bindingContext) = CreateBinder(null, null);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(20, result.Page); // DefaultPageSize used as page
        Assert.Equal(20, result.PageSize); // DefaultPageSize
        Assert.False(result.WasCapped);
    }

    [Theory]
    [InlineData(1, 2000, 1000)]
    [InlineData(1, 5000, 1000)]
    public async Task BindModelAsync_AppliesMaxPageSizeCapping(
        int page, int requestedPageSize, int expectedCappedSize)
    {
        // Arrange
        var (binder, bindingContext) = CreateBinder(page.ToString(), requestedPageSize.ToString());

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(expectedCappedSize, result.PageSize);
        Assert.True(result.WasCapped);
        Assert.Equal(expectedCappedSize, result.AppliedMaxPageSize);
    }

    [Fact]
    public async Task BindModelAsync_AsUserRole_CapsAt1000()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "user@test.com"),
            new Claim(ClaimTypes.Role, "User")
        };
        var (binder, bindingContext) = CreateBinder("1", "2000", claims);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(1000, result.PageSize);
        Assert.True(result.WasCapped);
    }

    [Fact]
    public async Task BindModelAsync_AsAdminRole_CapsAt5000()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "admin@test.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var (binder, bindingContext) = CreateBinder("1", "6000", claims);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(5000, result.PageSize);
        Assert.True(result.WasCapped);
    }

    [Fact]
    public async Task BindModelAsync_AsSuperAdminRole_CapsAt10000()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "superadmin@test.com"),
            new Claim(ClaimTypes.Role, "SuperAdmin")
        };
        var (binder, bindingContext) = CreateBinder("1", "15000", claims);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(10000, result.PageSize);
        Assert.True(result.WasCapped);
    }

    [Fact]
    public async Task BindModelAsync_WithMultipleRoles_UsesHighestLimit()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "user@test.com"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var (binder, bindingContext) = CreateBinder("1", "6000", claims);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(5000, result.PageSize); // Admin limit, higher than User
        Assert.True(result.WasCapped);
    }

    [Fact]
    public async Task BindModelAsync_WithEndpointOverride_UsesOverrideLimit()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "user@test.com"),
            new Claim(ClaimTypes.Role, "User")
        };
        var (binder, bindingContext) = CreateBinder("1", "6000", claims, "/api/v1/stock/overview");

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(5000, result.PageSize); // Endpoint override takes precedence
        Assert.True(result.WasCapped);
    }

    [Fact]
    public async Task BindModelAsync_WithWildcardEndpointOverride_Matches()
    {
        // Arrange
        var (binder, bindingContext) = CreateBinder("1", "15000", null, "/api/v1/export/products");

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(10000, result.PageSize); // Wildcard match for export endpoints
        Assert.True(result.WasCapped);
    }

    [Fact]
    public async Task BindModelAsync_WithExportOperationHeader_UsesExportLimit()
    {
        // Arrange
        var (binder, bindingContext) = CreateBinder("1", "15000", null, "/api/v1/test", true);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(10000, result.PageSize); // Export header allows MaxExportPageSize
        Assert.True(result.WasCapped);
    }

    [Fact]
    public async Task BindModelAsync_AboveRecommendedSize_LogsInformation()
    {
        // Arrange
        var (binder, bindingContext) = CreateBinder("1", "150");

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = bindingContext.Result.Model as PaginationParameters;
        Assert.NotNull(result);
        Assert.Equal(150, result.PageSize);
        Assert.False(result.WasCapped);

        // Verify information log was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("exceeds recommended size")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void CalculateSkip_ReturnsCorrectValue()
    {
        // Arrange & Act & Assert
        var page1 = new PaginationParameters(1, 20);
        Assert.Equal(0, page1.CalculateSkip());

        var page2 = new PaginationParameters(2, 20);
        Assert.Equal(20, page2.CalculateSkip());

        var page3 = new PaginationParameters(3, 50);
        Assert.Equal(100, page3.CalculateSkip());

        var page10 = new PaginationParameters(10, 25);
        Assert.Equal(225, page10.CalculateSkip());
    }

    [Fact]
    public async Task BindModelAsync_WhenCapped_LogsWarning()
    {
        // Arrange
        var (binder, bindingContext) = CreateBinder("1", "2000");

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("exceeds limit")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BindModelAsync_WithNullHttpContext_ReturnsFailed()
    {
        // Arrange
        var options = Options.Create(_settings);
        var binder = new PaginationModelBinder(options, _loggerMock.Object, _httpContextAccessorMock.Object);

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var mockMetadata = new Mock<ModelMetadata>(MockBehavior.Loose, ModelMetadataIdentity.ForType(typeof(PaginationParameters)));
        var bindingContext = new DefaultModelBindingContext
        {
            ModelName = "pagination",
            ModelMetadata = mockMetadata.Object,
            ValueProvider = new Mock<IValueProvider>().Object
        };

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.False(bindingContext.Result.IsModelSet);
    }

    private (PaginationModelBinder binder, DefaultModelBindingContext bindingContext) CreateBinder(
        string? page = null,
        string? pageSize = null,
        List<Claim>? claims = null,
        string path = "/api/v1/test",
        bool exportHeader = false)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        if (claims != null)
        {
            var identity = new ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        if (exportHeader)
        {
            httpContext.Request.Headers["X-Export-Operation"] = "true";
        }

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var options = Options.Create(_settings);
        var binder = new PaginationModelBinder(options, _loggerMock.Object, _httpContextAccessorMock.Object);

        var valueProvider = new Mock<IValueProvider>();
        valueProvider.Setup(x => x.GetValue("page"))
            .Returns(new ValueProviderResult(page ?? string.Empty));
        valueProvider.Setup(x => x.GetValue("pageSize"))
            .Returns(new ValueProviderResult(pageSize ?? string.Empty));

        var mockMetadata = new Mock<ModelMetadata>(MockBehavior.Loose, ModelMetadataIdentity.ForType(typeof(PaginationParameters)));
        var bindingContext = new DefaultModelBindingContext
        {
            ModelName = "pagination",
            ModelMetadata = mockMetadata.Object,
            ValueProvider = valueProvider.Object
        };

        return (binder, bindingContext);
    }
}
