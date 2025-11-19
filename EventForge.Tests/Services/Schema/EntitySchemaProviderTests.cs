using EventForge.Client.Services.Schema;
using EventForge.DTOs.Dashboard;

namespace EventForge.Tests.Services.Schema;

/// <summary>
/// Tests for the EntitySchemaProvider to ensure it correctly discovers and maps entity schemas.
/// </summary>
[Trait("Category", "Unit")]
public class EntitySchemaProviderTests
{
    private readonly IEntitySchemaProvider _provider;

    public EntitySchemaProviderTests()
    {
        _provider = new EntitySchemaProvider();
    }

    [Fact]
    public void GetSupportedEntityTypes_ShouldReturnKnownTypes()
    {
        // Act
        var types = _provider.GetSupportedEntityTypes();

        // Assert
        Assert.NotNull(types);
        Assert.Contains("VatRate", types);
        Assert.Contains("Product", types);
        Assert.Contains("BusinessParty", types);
    }

    [Fact]
    public void GetAvailableFields_VatRate_ShouldReturnExpectedFields()
    {
        // Act
        var fields = _provider.GetAvailableFields("VatRate", includeNested: false);

        // Assert
        Assert.NotNull(fields);
        Assert.NotEmpty(fields);
        
        // Verify key fields exist
        Assert.Contains(fields, f => f.Name == "Percentage");
        Assert.Contains(fields, f => f.Name == "Name");
        Assert.Contains(fields, f => f.Name == "Status");
        Assert.Contains(fields, f => f.Name == "IsActive");
    }

    [Fact]
    public void GetAvailableFields_Product_ShouldReturnExpectedFields()
    {
        // Act
        var fields = _provider.GetAvailableFields("Product", includeNested: false);

        // Assert
        Assert.NotNull(fields);
        Assert.NotEmpty(fields);
        
        // Verify key fields exist
        Assert.Contains(fields, f => f.Name == "Name");
        Assert.Contains(fields, f => f.Name == "Code");
        Assert.Contains(fields, f => f.Name == "DefaultPrice");
        Assert.Contains(fields, f => f.Name == "Status");
    }

    [Fact]
    public void GetCompatibleFields_Count_ShouldReturnEmpty()
    {
        // Act
        var fields = _provider.GetCompatibleFields("VatRate", MetricType.Count);

        // Assert
        Assert.NotNull(fields);
        Assert.Empty(fields);
    }

    [Theory]
    [InlineData(MetricType.Sum)]
    [InlineData(MetricType.Average)]
    [InlineData(MetricType.Min)]
    [InlineData(MetricType.Max)]
    public void GetCompatibleFields_NumericMetrics_ShouldReturnOnlyNumericFields(MetricType metricType)
    {
        // Act
        var fields = _provider.GetCompatibleFields("VatRate", metricType);

        // Assert
        Assert.NotNull(fields);
        Assert.NotEmpty(fields);
        
        // All returned fields should be numeric
        Assert.All(fields, f => Assert.True(f.IsNumeric, $"Field {f.Name} should be numeric"));
        
        // Percentage should be included as it's a decimal
        Assert.Contains(fields, f => f.Name == "Percentage");
    }

    [Fact]
    public void GetField_ValidField_ShouldReturnFieldMetadata()
    {
        // Act
        var field = _provider.GetField("VatRate", "Percentage");

        // Assert
        Assert.NotNull(field);
        Assert.Equal("Percentage", field.Name);
        Assert.Equal("Percentage", field.Path);
        Assert.Equal(FieldDataType.Decimal, field.DataType);
        Assert.True(field.IsNumeric);
        Assert.True(field.IsAggregatable);
    }

    [Fact]
    public void GetField_InvalidField_ShouldReturnNull()
    {
        // Act
        var field = _provider.GetField("VatRate", "NonExistentField");

        // Assert
        Assert.Null(field);
    }

    [Fact]
    public void GetAvailableFields_UnsupportedEntityType_ShouldReturnEmpty()
    {
        // Act
        var fields = _provider.GetAvailableFields("UnknownEntity", includeNested: false);

        // Assert
        Assert.NotNull(fields);
        Assert.Empty(fields);
    }

    [Fact]
    public void FieldMetadata_Percentage_ShouldHaveCorrectProperties()
    {
        // Act
        var field = _provider.GetField("VatRate", "Percentage");

        // Assert
        Assert.NotNull(field);
        Assert.Equal(FieldDataType.Decimal, field.DataType);
        Assert.NotNull(field.Description);
        Assert.NotEmpty(field.Examples);
        Assert.True(field.IsNumeric);
        Assert.True(field.IsAggregatable);
    }

    [Fact]
    public void FieldMetadata_Name_ShouldBeString()
    {
        // Act
        var field = _provider.GetField("VatRate", "Name");

        // Assert
        Assert.NotNull(field);
        Assert.Equal(FieldDataType.String, field.DataType);
        Assert.False(field.IsNumeric);
        Assert.False(field.IsAggregatable);
    }

    [Fact]
    public void FieldMetadata_IsActive_ShouldBeBoolean()
    {
        // Act
        var field = _provider.GetField("VatRate", "IsActive");

        // Assert
        Assert.NotNull(field);
        Assert.Equal(FieldDataType.Boolean, field.DataType);
        Assert.False(field.IsNumeric);
    }
}
