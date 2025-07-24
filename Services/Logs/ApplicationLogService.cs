using Dapper;
using EventForge.DTOs.Logs;
using Microsoft.Data.SqlClient;

namespace EventForge.Services.Logs;

/// <summary>
/// Service implementation for reading application logs from Serilog database.
/// </summary>
public class ApplicationLogService : IApplicationLogService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApplicationLogService> _logger;

    public ApplicationLogService(
        IConfiguration configuration,
        ILogger<ApplicationLogService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the connection string for the log database.
    /// </summary>
    private string GetLogConnectionString()
    {
        return _configuration.GetConnectionString("LogDb")
            ?? throw new InvalidOperationException("LogDb connection string not found.");
    }

    /// <inheritdoc />
    public async Task<PagedResult<ApplicationLogDto>> GetPagedLogsAsync(
        ApplicationLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = GetLogConnectionString();
            using var connection = new SqlConnection(connectionString);

            var whereClause = BuildWhereClause(queryParameters);
            var orderClause = BuildOrderClause(queryParameters);

            // Count query
            var countQuery = $@"
                SELECT COUNT(*)
                FROM Logs
                {whereClause}";

            // Data query with pagination
            var dataQuery = $@"
                SELECT Id, Message, MessageTemplate, Level, TimeStamp, Exception, Properties
                FROM Logs
                {whereClause}
                {orderClause}
                OFFSET {queryParameters.Skip} ROWS
                FETCH NEXT {queryParameters.PageSize} ROWS ONLY";

            await connection.OpenAsync(cancellationToken);

            // Get total count
            var totalCount = await connection.QuerySingleAsync<long>(countQuery, GetSqlParameters(queryParameters));

            // Get data
            var logs = await connection.QueryAsync<ApplicationLogDto>(dataQuery, GetSqlParameters(queryParameters));

            return new PagedResult<ApplicationLogDto>
            {
                Items = logs,
                Page = queryParameters.Page,
                PageSize = queryParameters.PageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated application logs");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApplicationLogDto?> GetLogByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = GetLogConnectionString();
            using var connection = new SqlConnection(connectionString);

            var query = @"
                SELECT Id, Message, MessageTemplate, Level, TimeStamp, Exception, Properties
                FROM Logs
                WHERE Id = @Id";

            await connection.OpenAsync(cancellationToken);
            var result = await connection.QueryFirstOrDefaultAsync<ApplicationLogDto>(query, new { Id = id });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application log by ID {LogId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApplicationLogDto>> GetLogsInDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = GetLogConnectionString();
            using var connection = new SqlConnection(connectionString);

            var query = @"
                SELECT Id, Message, MessageTemplate, Level, TimeStamp, Exception, Properties
                FROM Logs
                WHERE TimeStamp >= @FromDate AND TimeStamp <= @ToDate
                ORDER BY TimeStamp DESC";

            await connection.OpenAsync(cancellationToken);
            var result = await connection.QueryAsync<ApplicationLogDto>(query, new { FromDate = fromDate, ToDate = toDate });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application logs in date range {FromDate} - {ToDate}", fromDate, toDate);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ApplicationLogDto>> GetLogsByLevelAsync(
        string level,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = GetLogConnectionString();
            using var connection = new SqlConnection(connectionString);

            var query = @"
                SELECT Id, Message, MessageTemplate, Level, TimeStamp, Exception, Properties
                FROM Logs
                WHERE Level = @Level
                ORDER BY TimeStamp DESC";

            await connection.OpenAsync(cancellationToken);
            var result = await connection.QueryAsync<ApplicationLogDto>(query, new { Level = level });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving application logs by level {Level}", level);
            throw;
        }
    }

    /// <summary>
    /// Builds the WHERE clause for the SQL query based on query parameters.
    /// </summary>
    private static string BuildWhereClause(ApplicationLogQueryParameters parameters)
    {
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(parameters.Level))
        {
            conditions.Add("Level = @Level");
        }

        if (!string.IsNullOrWhiteSpace(parameters.Message))
        {
            conditions.Add("(Message LIKE @MessageSearch OR MessageTemplate LIKE @MessageSearch)");
        }

        if (parameters.FromDate.HasValue)
        {
            conditions.Add("TimeStamp >= @FromDate");
        }

        if (parameters.ToDate.HasValue)
        {
            conditions.Add("TimeStamp <= @ToDate");
        }

        if (parameters.HasException.HasValue)
        {
            if (parameters.HasException.Value)
            {
                conditions.Add("Exception IS NOT NULL AND Exception != ''");
            }
            else
            {
                conditions.Add("(Exception IS NULL OR Exception = '')");
            }
        }

        return conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
    }

    /// <summary>
    /// Builds the ORDER BY clause for the SQL query based on query parameters.
    /// </summary>
    private static string BuildOrderClause(ApplicationLogQueryParameters parameters)
    {
        var validSortFields = new[] { "Id", "TimeStamp", "Level", "Message" };
        var sortField = validSortFields.Contains(parameters.SortBy, StringComparer.OrdinalIgnoreCase)
            ? parameters.SortBy
            : "TimeStamp";

        var sortDirection = parameters.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";

        return $"ORDER BY {sortField} {sortDirection}";
    }

    /// <summary>
    /// Gets SQL parameters for the query based on query parameters.
    /// </summary>
    private static object GetSqlParameters(ApplicationLogQueryParameters parameters)
    {
        var parameterDict = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(parameters.Level))
        {
            parameterDict["Level"] = parameters.Level;
        }

        if (!string.IsNullOrWhiteSpace(parameters.Message))
        {
            parameterDict["MessageSearch"] = $"%{parameters.Message}%";
        }

        if (parameters.FromDate.HasValue)
        {
            parameterDict["FromDate"] = parameters.FromDate.Value;
        }

        if (parameters.ToDate.HasValue)
        {
            parameterDict["ToDate"] = parameters.ToDate.Value;
        }

        return parameterDict;
    }
}