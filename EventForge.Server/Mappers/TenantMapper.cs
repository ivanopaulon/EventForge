using EventForge.Server.Data.Entities.Auth;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for Tenant entity to DTOs.
/// </summary>
public static class TenantMapper
{
    /// <summary>
    /// Maps Tenant entity to TenantResponseDto.
    /// </summary>
    public static EventForge.DTOs.Tenants.TenantResponseDto ToResponseDto(Tenant tenant)
    {
        return new EventForge.DTOs.Tenants.TenantResponseDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            DisplayName = tenant.DisplayName,
            Description = tenant.Description,
            Domain = tenant.Domain,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            IsActive = tenant.IsEnabled,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.ModifiedAt ?? tenant.CreatedAt
        };
    }

    /// <summary>
    /// Maps collection of Tenant entities to TenantResponseDto collection.
    /// </summary>
    public static IEnumerable<EventForge.DTOs.Tenants.TenantResponseDto> ToResponseDtoCollection(IEnumerable<Tenant> tenants)
    {
        return tenants.Select(ToResponseDto);
    }
}