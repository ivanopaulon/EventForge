using EventForge.DTOs.Common;
using EventForge.DTOs.Sales;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service for managing tables and reservations in bar/restaurant scenarios.
/// </summary>
public interface ITableManagementService
{
    // Table Management
    /// <summary>
    /// Gets all tables with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of tables</returns>
    Task<PagedResult<TableSessionDto>> GetTablesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tables for specific zone with pagination.
    /// </summary>
    /// <param name="zone">Zone name</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of tables in zone</returns>
    Task<PagedResult<TableSessionDto>> GetTablesByZoneAsync(string zone, PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available tables with pagination (not occupied, reserved, or out of service).
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of available tables</returns>
    Task<PagedResult<TableSessionDto>> GetAvailableTablesAsync(PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tables for the current tenant (deprecated - use GetTablesAsync).
    /// </summary>
    [Obsolete("Use GetTablesAsync with pagination instead")]
    Task<List<TableSessionDto>> GetAllTablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific table by ID.
    /// </summary>
    Task<TableSessionDto?> GetTableAsync(Guid tableId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new table.
    /// </summary>
    Task<TableSessionDto> CreateTableAsync(CreateTableSessionDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates table information.
    /// </summary>
    Task<TableSessionDto?> UpdateTableAsync(Guid tableId, UpdateTableSessionDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates table status.
    /// </summary>
    Task<TableSessionDto?> UpdateTableStatusAsync(Guid tableId, UpdateTableStatusDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a table (soft delete).
    /// </summary>
    Task<bool> DeleteTableAsync(Guid tableId, CancellationToken cancellationToken = default);

    // Reservation Management
    /// <summary>
    /// Gets all reservations for a specific date.
    /// </summary>
    Task<List<TableReservationDto>> GetReservationsByDateAsync(DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific reservation by ID.
    /// </summary>
    Task<TableReservationDto?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new reservation.
    /// </summary>
    Task<TableReservationDto> CreateReservationAsync(CreateTableReservationDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a reservation.
    /// </summary>
    Task<TableReservationDto?> UpdateReservationAsync(Guid reservationId, UpdateTableReservationDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a reservation.
    /// </summary>
    Task<TableReservationDto?> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a reservation as arrived.
    /// </summary>
    Task<TableReservationDto?> MarkArrivedAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a reservation.
    /// </summary>
    Task<bool> CancelReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a reservation as no-show.
    /// </summary>
    Task<TableReservationDto?> MarkNoShowAsync(Guid reservationId, CancellationToken cancellationToken = default);
}
