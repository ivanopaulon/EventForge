using Prym.DTOs.Sales;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service for managing tables and reservations.
/// </summary>
public interface ITableManagementService
{
    /// <summary>
    /// Gets all tables.
    /// </summary>
    Task<List<TableSessionDto>?> GetAllTablesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a table by ID.
    /// </summary>
    Task<TableSessionDto?> GetTableAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all available tables.
    /// </summary>
    Task<List<TableSessionDto>?> GetAvailableTablesAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new table.
    /// </summary>
    Task<TableSessionDto?> CreateTableAsync(CreateTableSessionDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates a table.
    /// </summary>
    Task<TableSessionDto?> UpdateTableAsync(Guid id, UpdateTableSessionDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Updates a table's status.
    /// </summary>
    Task<TableSessionDto?> UpdateTableStatusAsync(Guid id, UpdateTableStatusDto statusDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a table.
    /// </summary>
    Task<bool> DeleteTableAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets reservations for a specific date.
    /// </summary>
    Task<List<TableReservationDto>?> GetReservationsByDateAsync(DateTime date, CancellationToken ct = default);

    /// <summary>
    /// Gets a reservation by ID.
    /// </summary>
    Task<TableReservationDto?> GetReservationAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new reservation.
    /// </summary>
    Task<TableReservationDto?> CreateReservationAsync(CreateTableReservationDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates a reservation.
    /// </summary>
    Task<TableReservationDto?> UpdateReservationAsync(Guid id, UpdateTableReservationDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Confirms a reservation.
    /// </summary>
    Task<TableReservationDto?> ConfirmReservationAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Marks a reservation as arrived.
    /// </summary>
    Task<TableReservationDto?> MarkArrivedAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Cancels a reservation.
    /// </summary>
    Task<bool> CancelReservationAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Marks a reservation as no-show.
    /// </summary>
    Task<TableReservationDto?> MarkNoShowAsync(Guid id, CancellationToken ct = default);
}
