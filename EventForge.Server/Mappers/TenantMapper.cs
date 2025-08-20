using ServerTenantDtos = EventForge.DTOs.Tenants;
using SharedTenantDtos = EventForge.DTOs.Tenants;
using EventForge.DTOs.Auth;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for Tenant entity to DTOs.
/// </summary>
public static class TenantMapper
{
    /// <summary>
    /// Maps Tenant entity to TenantDto for authentication.
    /// </summary>
    public static TenantDto ToDto(Tenant tenant)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            DisplayName = tenant.DisplayName,
            IsActive = tenant.IsActive
        };
    }

    /// <summary>
    /// Maps Tenant entity to TenantResponseDto.
    /// </summary>
    public static SharedTenantDtos.TenantResponseDto ToResponseDto(Tenant tenant)
    {
        return new SharedTenantDtos.TenantResponseDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            DisplayName = tenant.DisplayName,
            Description = tenant.Description,
            Domain = tenant.Domain,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.ModifiedAt ?? tenant.CreatedAt
        };
    }

    /// <summary>
    /// Maps Tenant entity to Server TenantResponseDto.
    /// </summary>
    public static ServerTenantDtos.TenantResponseDto ToServerResponseDto(Tenant tenant)
    {
        return new ServerTenantDtos.TenantResponseDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Code = tenant.Code,
            DisplayName = tenant.DisplayName,
            Description = tenant.Description,
            Domain = tenant.Domain,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            IsActive = tenant.IsActive,
            SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
            CreatedAt = tenant.CreatedAt,
            CreatedBy = tenant.CreatedBy,
            ModifiedAt = tenant.ModifiedAt,
            ModifiedBy = tenant.ModifiedBy
        };
    }

    /// <summary>
    /// Maps collection of Tenant entities to TenantResponseDto collection.
    /// </summary>
    public static IEnumerable<SharedTenantDtos.TenantResponseDto> ToResponseDtoCollection(IEnumerable<Tenant> tenants)
    {
        return tenants.Select(ToResponseDto);
    }

    /// <summary>
    /// Maps collection of Tenant entities to Server TenantResponseDto collection.
    /// </summary>
    public static IEnumerable<ServerTenantDtos.TenantResponseDto> ToServerResponseDtoCollection(IEnumerable<Tenant> tenants)
    {
        return tenants.Select(ToServerResponseDto);
    }
}