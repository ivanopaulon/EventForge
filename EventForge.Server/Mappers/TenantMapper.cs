using EventForge.Server.Data.Entities.Auth;
using EventForge.Server.DTOs.Tenants;

namespace EventForge.Server.Mappers;

/// <summary>
/// Manual mapper for Tenant entities and DTOs
/// </summary>
public static class TenantMapper
{
    /// <summary>
    /// Maps Tenant entity to TenantResponseDto
    /// </summary>
    public static TenantResponseDto ToResponseDto(Tenant entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new TenantResponseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            DisplayName = entity.DisplayName,
            Description = entity.Description,
            Domain = entity.Domain,
            ContactEmail = entity.ContactEmail,
            MaxUsers = entity.MaxUsers,
            IsEnabled = entity.IsEnabled,
            SubscriptionExpiresAt = entity.SubscriptionExpiresAt,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// Maps CreateTenantDto to Tenant entity
    /// </summary>
    public static Tenant ToEntity(CreateTenantDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Tenant
        {
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            Domain = dto.Domain,
            ContactEmail = dto.ContactEmail,
            MaxUsers = dto.MaxUsers,
            IsEnabled = true // Default to enabled as per mapping profile
            // Id, TenantId, CreatedAt, CreatedBy, AdminTenants are ignored as per mapping profile
        };
    }

    /// <summary>
    /// Updates Tenant entity with UpdateTenantDto data
    /// </summary>
    public static void UpdateEntity(Tenant entity, UpdateTenantDto dto)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Only update allowed fields as per mapping profile
        entity.DisplayName = dto.DisplayName;
        entity.Description = dto.Description;
        entity.Domain = dto.Domain;
        entity.ContactEmail = dto.ContactEmail;
        entity.MaxUsers = dto.MaxUsers;
        
        // Ignored fields: Id, TenantId, Name, IsEnabled, SubscriptionExpiresAt, 
        // CreatedAt, CreatedBy, AdminTenants
    }

    /// <summary>
    /// Maps a collection of Tenant entities to TenantResponseDto collection
    /// </summary>
    public static IEnumerable<TenantResponseDto> ToResponseDtoCollection(IEnumerable<Tenant> entities)
    {
        if (entities == null)
            return Enumerable.Empty<TenantResponseDto>();

        return entities.Select(ToResponseDto);
    }
}