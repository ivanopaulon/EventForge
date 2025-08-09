using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly EventForgeDbContext _context;
        private readonly ILogger<LogsController> _logger;

        public LogsController(EventForgeDbContext context, ILogger<LogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? level = null,
            [FromQuery] string? message = null,
            [FromQuery] string? sortBy = "TimeStamp",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var query = _context.LogEntries.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(level))
                {
                    query = query.Where(l => l.Level.Contains(level));
                }

                if (!string.IsNullOrEmpty(message))
                {
                    query = query.Where(l => l.Message.Contains(message));
                }

                // Apply sorting
                query = sortBy?.ToLowerInvariant() switch
                {
                    "level" => sortOrder?.ToLowerInvariant() == "desc"
                        ? query.OrderByDescending(l => l.Level)
                        : query.OrderBy(l => l.Level),
                    "message" => sortOrder?.ToLowerInvariant() == "desc"
                        ? query.OrderByDescending(l => l.Message)
                        : query.OrderBy(l => l.Message),
                    _ => sortOrder?.ToLowerInvariant() == "desc"
                        ? query.OrderByDescending(l => l.TimeStamp)
                        : query.OrderBy(l => l.TimeStamp)
                };

                var totalCount = await query.CountAsync();
                var logs = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new
                    {
                        l.Id,
                        l.TimeStamp,
                        l.Level,
                        l.Message,
                        l.Exception,
                        l.MachineName,
                        l.UserName
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Data = logs,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, new { Error = "Error retrieving logs" });
            }
        }

        [HttpGet("levels")]
        public async Task<IActionResult> GetLogLevels()
        {
            try
            {
                var levels = await _context.LogEntries
                    .Select(l => l.Level)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToListAsync();

                return Ok(levels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving log levels");
                return StatusCode(500, new { Error = "Error retrieving log levels" });
            }
        }
    }
}