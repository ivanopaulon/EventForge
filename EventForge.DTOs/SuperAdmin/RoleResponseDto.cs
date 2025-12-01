using System;

namespace EventForge.DTOs.SuperAdmin
{
    /// <summary>
    /// DTO for role response from the API.
    /// Used for deserializing role data in a strongly-typed manner.
    /// </summary>
    public class RoleResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
