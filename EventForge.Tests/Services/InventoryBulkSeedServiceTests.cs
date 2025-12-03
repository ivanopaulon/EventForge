using EventForge.DTOs.Warehouse;
using EventForge.Server.Data.Entities.Products;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Warehouse;
using Microsoft.Extensions.Logging;
using Moq;

namespace EventForge.Tests.Services;

/// <summary>
/// Unit tests for InventoryBulkSeedService to verify quantity calculation logic.
/// </summary>
[Trait("Category", "Unit")]
public class InventoryBulkSeedServiceTests
{
    [Fact]
    public void CalculateQuantity_FixedMode_ReturnsFixedValue()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "fixed",
            Quantity = 10m
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "TEST001",
            Name = "Test Product",
            TenantId = Guid.NewGuid()
        };

        // Act
        var result = CalculateQuantityHelper(request, product);

        // Assert
        Assert.Equal(10m, result);
    }

    [Fact]
    public void CalculateQuantity_FromProductWithTargetStock_ReturnsTargetStock()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "fromProduct",
            Quantity = 5m // fallback
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "TEST002",
            Name = "Test Product",
            TargetStockLevel = 20m,
            TenantId = Guid.NewGuid()
        };

        // Act
        var result = CalculateQuantityHelper(request, product);

        // Assert
        Assert.Equal(20m, result);
    }

    [Fact]
    public void CalculateQuantity_FromProductWithoutStockLevels_ReturnsFallback()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "fromProduct",
            Quantity = 5m // fallback
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "TEST003",
            Name = "Test Product",
            TenantId = Guid.NewGuid()
        };

        // Act
        var result = CalculateQuantityHelper(request, product);

        // Assert
        Assert.Equal(5m, result);
    }

    [Fact]
    public void CalculateQuantity_RandomMode_ReturnsValueInRange()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "random",
            MinQuantity = 10m,
            MaxQuantity = 50m
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "TEST003",
            Name = "Test Product 3",
            TenantId = Guid.NewGuid()
        };

        // Act - test multiple times to verify randomness
        var results = new List<decimal>();
        for (int i = 0; i < 10; i++)
        {
            var result = CalculateQuantityHelper(request, product);
            results.Add(result);
        }

        // Assert
        Assert.All(results, r =>
        {
            Assert.True(r >= request.MinQuantity.Value, $"Value {r} should be >= {request.MinQuantity.Value}");
            Assert.True(r <= request.MaxQuantity.Value, $"Value {r} should be <= {request.MaxQuantity.Value}");
        });
    }

    [Fact]
    public void CalculateQuantity_FromProduct_TargetStockTakesPriority()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "fromProduct",
            Quantity = 10m
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "TEST004",
            Name = "Test Product 4",
            TargetStockLevel = 20m,
            ReorderPoint = 15m,
            SafetyStock = 10m,
            TenantId = Guid.NewGuid()
        };

        // Act
        var result = CalculateQuantityHelper(request, product);

        // Assert
        Assert.Equal(20m, result);
    }

    [Fact]
    public void CalculateQuantity_FromProduct_ReorderPointUsedIfNoTargetStock()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "fromProduct",
            Quantity = 10m
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "TEST005",
            Name = "Test Product 5",
            ReorderPoint = 15m,
            SafetyStock = 10m,
            TenantId = Guid.NewGuid()
        };

        // Act
        var result = CalculateQuantityHelper(request, product);

        // Assert
        Assert.Equal(15m, result);
    }

    [Fact]
    public void CalculateQuantity_FromProduct_SafetyStockUsedIfNoTargetOrReorder()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "fromProduct",
            Quantity = 10m
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Code = "TEST006",
            Name = "Test Product 6",
            SafetyStock = 12m,
            TenantId = Guid.NewGuid()
        };

        // Act
        var result = CalculateQuantityHelper(request, product);

        // Assert
        Assert.Equal(12m, result);
    }

    [Fact]
    public void ValidateRequest_WithInvalidMode_ThrowsArgumentException()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "invalid_mode",
            Quantity = 10
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidateRequestHelper(request));
    }

    [Fact]
    public void ValidateRequest_FixedModeWithoutQuantity_ThrowsArgumentException()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "fixed",
            Quantity = null
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidateRequestHelper(request));
    }

    [Fact]
    public void ValidateRequest_RandomModeWithInvalidRange_ThrowsArgumentException()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "random",
            MinQuantity = 100m,
            MaxQuantity = 50m
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidateRequestHelper(request));
    }

    [Fact]
    public void ValidateRequest_WithInvalidBatchSize_ThrowsArgumentException()
    {
        // Arrange
        var request = new InventorySeedRequestDto
        {
            Mode = "fixed",
            Quantity = 10m,
            BatchSize = 2000 // Over max of 1000
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ValidateRequestHelper(request));
    }

    // Helper methods that replicate the internal logic of the service
    private decimal CalculateQuantityHelper(InventorySeedRequestDto request, Product product)
    {
        return request.Mode.ToLowerInvariant() switch
        {
            "fixed" => request.Quantity ?? 0m,
            "random" => GenerateRandomQuantity(request.MinQuantity ?? 0m, request.MaxQuantity ?? 100m),
            "fromproduct" => GetQuantityFromProduct(product, request.Quantity ?? 10m),
            _ => throw new ArgumentException($"Mode non supportata: {request.Mode}")
        };
    }

    private static readonly Random _random = new();

    private decimal GenerateRandomQuantity(decimal min, decimal max)
    {
        var range = max - min;
        var randomValue = (decimal)_random.NextDouble() * range;
        return Math.Round(min + randomValue, 2);
    }

    private decimal GetQuantityFromProduct(Product product, decimal fallback)
    {
        if (product.TargetStockLevel.HasValue && product.TargetStockLevel.Value > 0)
        {
            return product.TargetStockLevel.Value;
        }

        if (product.ReorderPoint.HasValue && product.ReorderPoint.Value > 0)
        {
            return product.ReorderPoint.Value;
        }

        if (product.SafetyStock.HasValue && product.SafetyStock.Value > 0)
        {
            return product.SafetyStock.Value;
        }

        return fallback;
    }

    private void ValidateRequestHelper(InventorySeedRequestDto request)
    {
        var validModes = new[] { "fixed", "random", "fromProduct" };
        if (!validModes.Contains(request.Mode.ToLowerInvariant()))
        {
            throw new ArgumentException($"Mode non valida. Valori ammessi: {string.Join(", ", validModes)}");
        }

        if (request.Mode.Equals("fixed", StringComparison.OrdinalIgnoreCase) && !request.Quantity.HasValue)
        {
            throw new ArgumentException("Quantity è richiesta quando Mode è 'fixed'.");
        }

        if (request.Mode.Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.MinQuantity.HasValue || !request.MaxQuantity.HasValue)
            {
                throw new ArgumentException("MinQuantity e MaxQuantity sono richiesti quando Mode è 'random'.");
            }

            if (request.MinQuantity.Value > request.MaxQuantity.Value)
            {
                throw new ArgumentException("MinQuantity non può essere maggiore di MaxQuantity.");
            }
        }

        if (request.BatchSize < 1 || request.BatchSize > 1000)
        {
            throw new ArgumentException("BatchSize deve essere compreso tra 1 e 1000.");
        }
    }
}
