using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Logging;

/// <summary>
/// Service implementation for managing application logs.
/// </summary>
public class ApplicationLogService(
    EventForgeDbContext context,
    ILogger<ApplicationLogService> logger) : IApplicationLogService
{

    /// <summary>
    /// Gets all application logs with pagination.
    /// </summary>
    public async Task<PagedResult<DTOs.Logging.ApplicationLogDto>> GetApplicationLogsAsync(
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        try
        {
            var query = context.LogEntries.AsNoTracking().AsQueryable();

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(log => log.TimeStamp)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(log => new DTOs.Logging.ApplicationLogDto
                {
                    Id = log.Id,
                    TimeStamp = log.TimeStamp,
                    Level = log.Level,
                    Message = log.Message,
                    Exception = log.Exception,
                    MachineName = log.MachineName,
                    UserName = log.UserName
                })
                .ToListAsync(ct);

            return new PagedResult<DTOs.Logging.ApplicationLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets application logs for a specific log level with pagination.
    /// </summary>
    public async Task<PagedResult<DTOs.Logging.ApplicationLogDto>> GetLogsByLevelAsync(
        string level,
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        try
        {
            var query = context.LogEntries.AsNoTracking()
                .Where(log => log.Level == level);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(log => log.TimeStamp)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(log => new DTOs.Logging.ApplicationLogDto
                {
                    Id = log.Id,
                    TimeStamp = log.TimeStamp,
                    Level = log.Level,
                    Message = log.Message,
                    Exception = log.Exception,
                    MachineName = log.MachineName,
                    UserName = log.UserName
                })
                .ToListAsync(ct);

            return new PagedResult<DTOs.Logging.ApplicationLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets application logs within a date range with pagination.
    /// </summary>
    public async Task<PagedResult<DTOs.Logging.ApplicationLogDto>> GetLogsByDateRangeAsync(
        DateTime startDate,
        DateTime? endDate,
        PaginationParameters pagination,
        CancellationToken ct = default)
    {
        try
        {
            var end = endDate ?? DateTime.UtcNow;

            var query = context.LogEntries.AsNoTracking()
                .Where(log => log.TimeStamp >= startDate && log.TimeStamp <= end);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(log => log.TimeStamp)
                .Skip(pagination.CalculateSkip())
                .Take(pagination.PageSize)
                .Select(log => new DTOs.Logging.ApplicationLogDto
                {
                    Id = log.Id,
                    TimeStamp = log.TimeStamp,
                    Level = log.Level,
                    Message = log.Message,
                    Exception = log.Exception,
                    MachineName = log.MachineName,
                    UserName = log.UserName
                })
                .ToListAsync(ct);

            return new PagedResult<DTOs.Logging.ApplicationLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
