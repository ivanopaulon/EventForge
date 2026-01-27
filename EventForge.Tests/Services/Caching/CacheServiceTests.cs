using EventForge.Server.Services.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services.Caching;

/// <summary>
/// Tests for CacheService to verify multi-tenant isolation, cache invalidation, and memory management
/// </summary>
[Trait("Category", "Unit")]
public class CacheServiceTests
{
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CacheService>> _loggerMock;
    private readonly CacheService _service;

    public CacheServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100 // Small limit for testing
        });
        _loggerMock = new Mock<ILogger<CacheService>>();
        _service = new CacheService(_cache, _loggerMock.Object);
    }

    [Fact]
    public async Task GetOrCreateAsync_FirstCall_ExecutesFactory()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var factoryCallCount = 0;
        var expectedValue = "TestValue";

        // Act
        var result = await _service.GetOrCreateAsync(
            "TestKey",
            tenantId,
            () =>
            {
                factoryCallCount++;
                return Task.FromResult(expectedValue);
            }
        );

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Equal(1, factoryCallCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_SecondCall_ReturnsCachedValue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var factoryCallCount = 0;
        var expectedValue = "TestValue";

        // Act - First call
        var result1 = await _service.GetOrCreateAsync(
            "TestKey",
            tenantId,
            () =>
            {
                factoryCallCount++;
                return Task.FromResult(expectedValue);
            }
        );

        // Act - Second call
        var result2 = await _service.GetOrCreateAsync(
            "TestKey",
            tenantId,
            () =>
            {
                factoryCallCount++;
                return Task.FromResult("DifferentValue");
            }
        );

        // Assert
        Assert.Equal(expectedValue, result1);
        Assert.Equal(expectedValue, result2);
        Assert.Equal(1, factoryCallCount); // Factory should only be called once
    }

    [Fact]
    public async Task GetOrCreateAsync_DifferentTenants_IsolatesData()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var value1 = "Tenant1Data";
        var value2 = "Tenant2Data";

        // Act
        var result1 = await _service.GetOrCreateAsync(
            "TestKey",
            tenant1,
            () => Task.FromResult(value1)
        );

        var result2 = await _service.GetOrCreateAsync(
            "TestKey",
            tenant2,
            () => Task.FromResult(value2)
        );

        // Assert - Different tenants should have different cached values
        Assert.Equal(value1, result1);
        Assert.Equal(value2, result2);
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public async Task Invalidate_RemovesCachedValue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var factoryCallCount = 0;
        var expectedValue = "TestValue";

        // Pre-populate cache
        await _service.GetOrCreateAsync(
            "TestKey",
            tenantId,
            () =>
            {
                factoryCallCount++;
                return Task.FromResult(expectedValue);
            }
        );

        // Act - Invalidate cache
        _service.Invalidate("TestKey", tenantId);

        // Get value again - should call factory again
        await _service.GetOrCreateAsync(
            "TestKey",
            tenantId,
            () =>
            {
                factoryCallCount++;
                return Task.FromResult(expectedValue);
            }
        );

        // Assert - Factory should have been called twice
        Assert.Equal(2, factoryCallCount);
    }

    [Fact]
    public async Task Invalidate_OnlyInvalidatesSpecificTenant()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var factoryCallCount1 = 0;
        var factoryCallCount2 = 0;

        // Pre-populate cache for both tenants
        await _service.GetOrCreateAsync(
            "TestKey",
            tenant1,
            () =>
            {
                factoryCallCount1++;
                return Task.FromResult("Tenant1Data");
            }
        );

        await _service.GetOrCreateAsync(
            "TestKey",
            tenant2,
            () =>
            {
                factoryCallCount2++;
                return Task.FromResult("Tenant2Data");
            }
        );

        // Act - Invalidate only tenant1's cache
        _service.Invalidate("TestKey", tenant1);

        // Get values again
        await _service.GetOrCreateAsync(
            "TestKey",
            tenant1,
            () =>
            {
                factoryCallCount1++;
                return Task.FromResult("Tenant1Data");
            }
        );

        await _service.GetOrCreateAsync(
            "TestKey",
            tenant2,
            () =>
            {
                factoryCallCount2++;
                return Task.FromResult("Tenant2Data");
            }
        );

        // Assert
        Assert.Equal(2, factoryCallCount1); // Tenant1 cache was invalidated
        Assert.Equal(1, factoryCallCount2); // Tenant2 cache was not invalidated
    }

    [Fact]
    public async Task InvalidateTenant_RemovesAllCacheEntriesForTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var factoryCallCount1 = 0;
        var factoryCallCount2 = 0;

        // Pre-populate cache with multiple keys
        await _service.GetOrCreateAsync(
            "TestKey1",
            tenantId,
            () =>
            {
                factoryCallCount1++;
                return Task.FromResult("Value1");
            }
        );

        await _service.GetOrCreateAsync(
            "TestKey2",
            tenantId,
            () =>
            {
                factoryCallCount2++;
                return Task.FromResult("Value2");
            }
        );

        // Act - Invalidate all entries for tenant
        _service.InvalidateTenant(tenantId);

        // Get values again
        await _service.GetOrCreateAsync(
            "TestKey1",
            tenantId,
            () =>
            {
                factoryCallCount1++;
                return Task.FromResult("Value1");
            }
        );

        await _service.GetOrCreateAsync(
            "TestKey2",
            tenantId,
            () =>
            {
                factoryCallCount2++;
                return Task.FromResult("Value2");
            }
        );

        // Assert - Both factories should have been called twice
        Assert.Equal(2, factoryCallCount1);
        Assert.Equal(2, factoryCallCount2);
    }

    [Fact]
    public async Task GetOrCreateAsync_RespectsAbsoluteExpiration()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var factoryCallCount = 0;

        // Act - Create cache entry with very short expiration
        await _service.GetOrCreateAsync(
            "TestKey",
            tenantId,
            () =>
            {
                factoryCallCount++;
                return Task.FromResult("TestValue");
            },
            absoluteExpiration: TimeSpan.FromMilliseconds(100)
        );

        // Wait for expiration
        await Task.Delay(150);

        // Get value again - should call factory again due to expiration
        await _service.GetOrCreateAsync(
            "TestKey",
            tenantId,
            () =>
            {
                factoryCallCount++;
                return Task.FromResult("TestValue");
            },
            absoluteExpiration: TimeSpan.FromMilliseconds(100)
        );

        // Assert - Factory should have been called twice
        Assert.Equal(2, factoryCallCount);
    }

    [Fact]
    public async Task GetOrCreateAsync_HandlesComplexObjects()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var complexObject = new List<string> { "Item1", "Item2", "Item3" };

        // Act
        var result = await _service.GetOrCreateAsync(
            "TestKey",
            tenantId,
            () => Task.FromResult(complexObject)
        );

        // Assert
        Assert.Equal(complexObject, result);
        Assert.Equal(3, result.Count);
    }
}
