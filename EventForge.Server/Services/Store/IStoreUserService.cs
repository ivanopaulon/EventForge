using EventForge.DTOs.Store;

namespace EventForge.Server.Services.Store;

/// <summary>
/// Service interface for managing store users, groups, and privileges.
/// </summary>
public interface IStoreUserService
{
    // StoreUser CRUD operations

    /// <summary>
    /// Gets all store users with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store users</returns>
    Task<PagedResult<StoreUserDto>> GetStoreUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a store user by ID.
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store user DTO or null if not found</returns>
    Task<StoreUserDto?> GetStoreUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets store users by group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of store users in the group</returns>
    Task<IEnumerable<StoreUserDto>> GetStoreUsersByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new store user.
    /// </summary>
    /// <param name="createStoreUserDto">Store user creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created store user DTO</returns>
    Task<StoreUserDto> CreateStoreUserAsync(CreateStoreUserDto createStoreUserDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing store user.
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="updateStoreUserDto">Store user update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user DTO or null if not found</returns>
    Task<StoreUserDto?> UpdateStoreUserAsync(Guid id, UpdateStoreUserDto updateStoreUserDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a store user (soft delete).
    /// </summary>
    /// <param name="id">Store user ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteStoreUserAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // StoreUserGroup CRUD operations

    /// <summary>
    /// Gets all store user groups with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store user groups</returns>
    Task<PagedResult<StoreUserGroupDto>> GetStoreUserGroupsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a store user group by ID.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store user group DTO or null if not found</returns>
    Task<StoreUserGroupDto?> GetStoreUserGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new store user group.
    /// </summary>
    /// <param name="createStoreUserGroupDto">Store user group creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created store user group DTO</returns>
    Task<StoreUserGroupDto> CreateStoreUserGroupAsync(CreateStoreUserGroupDto createStoreUserGroupDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing store user group.
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="updateStoreUserGroupDto">Store user group update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user group DTO or null if not found</returns>
    Task<StoreUserGroupDto?> UpdateStoreUserGroupAsync(Guid id, UpdateStoreUserGroupDto updateStoreUserGroupDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a store user group (soft delete).
    /// </summary>
    /// <param name="id">Store user group ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteStoreUserGroupAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // StoreUserPrivilege CRUD operations

    /// <summary>
    /// Gets all store user privileges with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store user privileges</returns>
    Task<PagedResult<StoreUserPrivilegeDto>> GetStoreUserPrivilegesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a store user privilege by ID.
    /// </summary>
    /// <param name="id">Store user privilege ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store user privilege DTO or null if not found</returns>
    Task<StoreUserPrivilegeDto?> GetStoreUserPrivilegeByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets store user privileges by group.
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of store user privileges for the group</returns>
    Task<IEnumerable<StoreUserPrivilegeDto>> GetStoreUserPrivilegesByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new store user privilege.
    /// </summary>
    /// <param name="createStoreUserPrivilegeDto">Store user privilege creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created store user privilege DTO</returns>
    Task<StoreUserPrivilegeDto> CreateStoreUserPrivilegeAsync(CreateStoreUserPrivilegeDto createStoreUserPrivilegeDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing store user privilege.
    /// </summary>
    /// <param name="id">Store user privilege ID</param>
    /// <param name="updateStoreUserPrivilegeDto">Store user privilege update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user privilege DTO or null if not found</returns>
    Task<StoreUserPrivilegeDto?> UpdateStoreUserPrivilegeAsync(Guid id, UpdateStoreUserPrivilegeDto updateStoreUserPrivilegeDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a store user privilege (soft delete).
    /// </summary>
    /// <param name="id">Store user privilege ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteStoreUserPrivilegeAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a store user exists.
    /// </summary>
    /// <param name="storeUserId">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> StoreUserExistsAsync(Guid storeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a store user group exists.
    /// </summary>
    /// <param name="groupId">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> StoreUserGroupExistsAsync(Guid groupId, CancellationToken cancellationToken = default);

    // Image Management Operations - Issue #315

    /// <summary>
    /// Uploads a photo for a store user (with GDPR consent validation).
    /// </summary>
    /// <param name="storeUserId">Store user ID</param>
    /// <param name="file">Photo file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user DTO or null if not found</returns>
    Task<StoreUserDto?> UploadStoreUserPhotoAsync(Guid storeUserId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the photo DocumentReference for a store user.
    /// </summary>
    /// <param name="storeUserId">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Photo document reference or null if not found</returns>
    Task<EventForge.DTOs.Teams.DocumentReferenceDto?> GetStoreUserPhotoDocumentAsync(Guid storeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the photo for a store user.
    /// </summary>
    /// <param name="storeUserId">Store user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteStoreUserPhotoAsync(Guid storeUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a logo for a store user group.
    /// </summary>
    /// <param name="groupId">Store user group ID</param>
    /// <param name="file">Logo file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store user group DTO or null if not found</returns>
    Task<StoreUserGroupDto?> UploadStoreUserGroupLogoAsync(Guid groupId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the logo DocumentReference for a store user group.
    /// </summary>
    /// <param name="groupId">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logo document reference or null if not found</returns>
    Task<EventForge.DTOs.Teams.DocumentReferenceDto?> GetStoreUserGroupLogoDocumentAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the logo for a store user group.
    /// </summary>
    /// <param name="groupId">Store user group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteStoreUserGroupLogoAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an image for a store POS.
    /// </summary>
    /// <param name="storePosId">Store POS ID</param>
    /// <param name="file">Image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store POS DTO or null if not found</returns>
    Task<StorePosDto?> UploadStorePosImageAsync(Guid storePosId, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the image DocumentReference for a store POS.
    /// </summary>
    /// <param name="storePosId">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image document reference or null if not found</returns>
    Task<EventForge.DTOs.Teams.DocumentReferenceDto?> GetStorePosImageDocumentAsync(Guid storePosId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the image for a store POS.
    /// </summary>
    /// <param name="storePosId">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteStorePosImageAsync(Guid storePosId, CancellationToken cancellationToken = default);
}