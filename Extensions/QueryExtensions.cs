using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EventForge.Extensions;

/// <summary>
/// Extension methods for performance optimizations in data access.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Applies AsNoTracking to queries for read-only scenarios to improve performance.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">The query</param>
    /// <returns>Query with no tracking enabled</returns>
    public static IQueryable<T> AsReadOnly<T>(this IQueryable<T> query) where T : class
    {
        return query.AsNoTracking();
    }

    /// <summary>
    /// Applies pagination with performance optimizations.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">The query</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated query</returns>
    public static IQueryable<T> PageBy<T>(this IQueryable<T> query, int page, int pageSize) where T : class
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Limit max page size for performance

        return query.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Applies pagination with total count for complete paging information.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">The query</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated result with total count</returns>
    public static async Task<(IList<T> Items, int TotalCount, int TotalPages)> ToPagedResultAsync<T>(
        this IQueryable<T> query, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default) where T : class
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, totalPages);
    }

    /// <summary>
    /// Applies AsNoTracking and includes related entities for read-only scenarios.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <typeparam name="TProperty">Property type</typeparam>
    /// <param name="query">The query</param>
    /// <param name="navigationPropertyPath">Navigation property to include</param>
    /// <returns>Query with include and no tracking</returns>
    public static IQueryable<T> IncludeReadOnly<T, TProperty>(
        this IQueryable<T> query,
        Expression<Func<T, TProperty>> navigationPropertyPath) where T : class
    {
        return query.Include(navigationPropertyPath).AsNoTracking();
    }

    /// <summary>
    /// Applies multiple includes for read-only scenarios.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">The query</param>
    /// <param name="includes">Navigation properties to include</param>
    /// <returns>Query with multiple includes and no tracking</returns>
    public static IQueryable<T> IncludeMultipleReadOnly<T>(
        this IQueryable<T> query,
        params Expression<Func<T, object>>[] includes) where T : class
    {
        var result = query;
        foreach (var include in includes)
        {
            result = result.Include(include);
        }
        return result.AsNoTracking();
    }

    /// <summary>
    /// Applies conditional where clause for better query composition.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">The query</param>
    /// <param name="condition">Condition to check</param>
    /// <param name="predicate">Where clause predicate</param>
    /// <returns>Query with conditional where clause</returns>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate) where T : class
    {
        return condition ? query.Where(predicate) : query;
    }

    /// <summary>
    /// Applies efficient exists check without loading data.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="query">The query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entities match the query</returns>
    public static async Task<bool> ExistsAsync<T>(
        this IQueryable<T> query,
        CancellationToken cancellationToken = default) where T : class
    {
        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Gets only specific columns for better performance (projection).
    /// </summary>
    /// <typeparam name="TSource">Source entity type</typeparam>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="query">The query</param>
    /// <param name="selector">Column selector</param>
    /// <returns>Query with projection</returns>
    public static IQueryable<TResult> SelectColumns<TSource, TResult>(
        this IQueryable<TSource> query,
        Expression<Func<TSource, TResult>> selector) where TSource : class
    {
        return query.AsNoTracking().Select(selector);
    }

    /// <summary>
    /// Applies active entity filter for auditable entities.
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from AuditableEntity</typeparam>
    /// <param name="query">The query</param>
    /// <returns>Query filtered to active entities</returns>
    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query) where T : AuditableEntity
    {
        return query.Where(e => e.IsActive && !e.IsDeleted);
    }

    /// <summary>
    /// Orders by creation date descending for auditable entities.
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from AuditableEntity</typeparam>
    /// <param name="query">The query</param>
    /// <returns>Query ordered by creation date descending</returns>
    public static IQueryable<T> OrderByNewest<T>(this IQueryable<T> query) where T : AuditableEntity
    {
        return query.OrderByDescending(e => e.CreatedAt);
    }

    /// <summary>
    /// Orders by last modified date descending for auditable entities.
    /// </summary>
    /// <typeparam name="T">Entity type that inherits from AuditableEntity</typeparam>
    /// <param name="query">The query</param>
    /// <returns>Query ordered by modification date descending</returns>
    public static IQueryable<T> OrderByLastModified<T>(this IQueryable<T> query) where T : AuditableEntity
    {
        return query.OrderByDescending(e => e.ModifiedAt ?? e.CreatedAt);
    }
}