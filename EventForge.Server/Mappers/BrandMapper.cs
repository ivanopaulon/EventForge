using EventForge.DTOs.Products;
using EventForge.Server.Data.Entities.Products;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for Brand entity to DTOs.
/// </summary>
public static class BrandMapper
{
    /// <summary>
    /// Maps Brand entity to BrandDto.
    /// </summary>
    public static BrandDto ToDto(Brand brand)
    {
        return new BrandDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Description = brand.Description,
            Website = brand.Website,
            Country = brand.Country,
            CreatedAt = brand.CreatedAt,
            CreatedBy = brand.CreatedBy
        };
    }

    /// <summary>
    /// Maps collection of Brand entities to BrandDto collection.
    /// </summary>
    public static IEnumerable<BrandDto> ToDtoCollection(IEnumerable<Brand> brands)
    {
        return brands.Select(ToDto);
    }

    /// <summary>
    /// Maps collection of Brand entities to BrandDto list.
    /// </summary>
    public static List<BrandDto> ToDtoList(IEnumerable<Brand> brands)
    {
        return brands.Select(ToDto).ToList();
    }

    /// <summary>
    /// Maps CreateBrandDto to Brand entity.
    /// </summary>
    public static Brand ToEntity(CreateBrandDto dto)
    {
        return new Brand
        {
            Name = dto.Name,
            Description = dto.Description,
            Website = dto.Website,
            Country = dto.Country
        };
    }

    /// <summary>
    /// Updates Brand entity from UpdateBrandDto.
    /// </summary>
    public static void UpdateEntity(Brand entity, UpdateBrandDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Website = dto.Website;
        entity.Country = dto.Country;
    }
}
