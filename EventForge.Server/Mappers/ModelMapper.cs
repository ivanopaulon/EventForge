using EventForge.DTOs.Products;
using EventForge.Server.Data.Entities.Products;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for Model entity to DTOs.
/// </summary>
public static class ModelMapper
{
    /// <summary>
    /// Maps Model entity to ModelDto.
    /// </summary>
    public static ModelDto ToDto(Model model)
    {
        return new ModelDto
        {
            Id = model.Id,
            BrandId = model.BrandId,
            BrandName = model.Brand?.Name,
            Name = model.Name,
            Description = model.Description,
            ManufacturerPartNumber = model.ManufacturerPartNumber,
            CreatedAt = model.CreatedAt,
            CreatedBy = model.CreatedBy
        };
    }

    /// <summary>
    /// Maps collection of Model entities to ModelDto collection.
    /// </summary>
    public static IEnumerable<ModelDto> ToDtoCollection(IEnumerable<Model> models)
    {
        return models.Select(ToDto);
    }

    /// <summary>
    /// Maps collection of Model entities to ModelDto list.
    /// </summary>
    public static List<ModelDto> ToDtoList(IEnumerable<Model> models)
    {
        return models.Select(ToDto).ToList();
    }

    /// <summary>
    /// Maps CreateModelDto to Model entity.
    /// </summary>
    public static Model ToEntity(CreateModelDto dto)
    {
        return new Model
        {
            BrandId = dto.BrandId,
            Name = dto.Name,
            Description = dto.Description,
            ManufacturerPartNumber = dto.ManufacturerPartNumber
        };
    }

    /// <summary>
    /// Updates Model entity from UpdateModelDto.
    /// </summary>
    public static void UpdateEntity(Model entity, UpdateModelDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.ManufacturerPartNumber = dto.ManufacturerPartNumber;
    }
}
