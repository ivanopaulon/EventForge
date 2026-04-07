using EventForge.DTOs.Warehouse;
using System.ComponentModel.DataAnnotations;

namespace EventForge.Tests.DTOs;

/// <summary>
/// Tests for StorageLocation DTOs to ensure validation rules work correctly.
/// </summary>
[Trait("Category", "Unit")]
public class StorageLocationDtoTests
{
    [Fact]
    public void CreateStorageLocationDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new CreateStorageLocationDto
        {
            Code = "A-01",
            Description = "Test location",
            WarehouseId = Guid.NewGuid(),
            Capacity = 100,
            Occupancy = 50,
            IsRefrigerated = false,
            Notes = "Test notes",
            Zone = "A",
            Floor = "1",
            Row = "01",
            Column = "A",
            Level = "1"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreateStorageLocationDto_WithEmptyCode_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateStorageLocationDto
        {
            Code = "",
            WarehouseId = Guid.NewGuid()
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("Code"));
    }

    [Fact]
    public void CreateStorageLocationDto_WithEmptyWarehouseId_ShouldStillPassDataAnnotationValidation()
    {
        // Note: [Required] attribute on Guid doesn't catch Guid.Empty
        // This is why we need client-side validation in the drawer component
        // Arrange
        var dto = new CreateStorageLocationDto
        {
            Code = "A-01",
            WarehouseId = Guid.Empty
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        // Guid.Empty passes DataAnnotations validation (this is a known limitation)
        // That's why we added explicit validation in StorageLocationDrawer.HandleSave()
        Assert.True(isValid);
    }

    [Fact]
    public void CreateStorageLocationDto_WithCodeTooLong_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateStorageLocationDto
        {
            Code = new string('A', 31), // 31 characters (max is 30)
            WarehouseId = Guid.NewGuid()
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("Code"));
    }

    [Fact]
    public void CreateStorageLocationDto_WithNegativeCapacity_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateStorageLocationDto
        {
            Code = "A-01",
            WarehouseId = Guid.NewGuid(),
            Capacity = -1
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("Capacity"));
    }

    [Fact]
    public void UpdateStorageLocationDto_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var dto = new UpdateStorageLocationDto
        {
            Code = "A-01-UPDATED",
            Description = "Updated location",
            WarehouseId = Guid.NewGuid(),
            Capacity = 200,
            Occupancy = 75,
            IsRefrigerated = true,
            IsActive = true
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void UpdateStorageLocationDto_WithCodeTooLong_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateStorageLocationDto
        {
            Code = new string('B', 31) // 31 characters (max is 30)
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("Code"));
    }

    [Fact]
    public void UpdateStorageLocationDto_WithNegativeOccupancy_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateStorageLocationDto
        {
            Occupancy = -10
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(dto);
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains("Occupancy"));
    }
}
