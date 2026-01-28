using AutoMapper;
using AutoMapper.QueryableExtensions;
using EventForge.DTOs.Common;
using EventForge.DTOs.Users;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Users;

/// <summary>
/// Service implementation for managing users.
/// </summary>
public class UserService : IUserService
{
    private readonly EventForgeDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UserService> _logger;
    private readonly IMapper _mapper;

    public UserService(
        EventForgeDbContext context,
        ITenantContext tenantContext,
        ILogger<UserService> logger,
        IMapper mapper)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Gets all users with pagination.
    /// </summary>
    public async Task<PagedResult<UserDto>> GetUsersAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted && u.TenantId == _tenantContext.CurrentTenantId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Gets users by role with pagination.
    /// </summary>
    public async Task<PagedResult<UserDto>> GetUsersByRoleAsync(
        string role,
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted
                && u.TenantId == _tenantContext.CurrentTenantId
                && u.UserRoles.Any(ur => ur.Role.Name == role));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    /// <summary>
    /// Gets active users with pagination.
    /// </summary>
    public async Task<PagedResult<UserDto>> GetActiveUsersAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => !u.IsDeleted
                && u.TenantId == _tenantContext.CurrentTenantId
                && u.IsActive);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip(pagination.CalculateSkip())
            .Take(pagination.PageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.FullName,
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }
}
