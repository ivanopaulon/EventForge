namespace EventForge.DTOs.SuperAdmin
{
    /// <summary>
    /// DTO for updating role permissions.
    /// </summary>
    public class UpdateRolePermissionsDto
    {
        /// <summary>
        /// List of permission IDs to assign to the role.
        /// </summary>
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();
    }
}
