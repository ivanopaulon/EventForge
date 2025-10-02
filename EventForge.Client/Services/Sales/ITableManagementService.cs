using EventForge.DTOs.Sales;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service for managing tables and reservations.
/// </summary>
public interface ITableManagementService
{
    /// <summary>
    /// Gets all tables.
    /// </summary>
    Task<List<TableSessionDto>?> GetAllTablesAsync();

    /// <summary>
    /// Gets a table by ID.
    /// </summary>
    Task<TableSessionDto?> GetTableAsync(Guid id);

    /// <summary>
    /// Gets all available tables.
    /// </summary>
    Task<List<TableSessionDto>?> GetAvailableTablesAsync();

    /// <summary>
    /// Creates a new table.
    /// </summary>
    Task<TableSessionDto?> CreateTableAsync(CreateTableSessionDto createDto);

    /// <summary>
    /// Updates a table.
    /// </summary>
    Task<TableSessionDto?> UpdateTableAsync(Guid id, UpdateTableSessionDto updateDto);

    /// <summary>
    /// Updates a table's status.
    /// </summary>
    Task<TableSessionDto?> UpdateTableStatusAsync(Guid id, UpdateTableStatusDto statusDto);

    /// <summary>
    /// Deletes a table.
    /// </summary>
    Task<bool> DeleteTableAsync(Guid id);

    /// <summary>
    /// Gets reservations for a specific date.
    /// </summary>
    Task<List<TableReservationDto>?> GetReservationsByDateAsync(DateTime date);

    /// <summary>
    /// Gets a reservation by ID.
    /// </summary>
    Task<TableReservationDto?> GetReservationAsync(Guid id);

    /// <summary>
    /// Creates a new reservation.
    /// </summary>
    Task<TableReservationDto?> CreateReservationAsync(CreateTableReservationDto createDto);

    /// <summary>
    /// Updates a reservation.
    /// </summary>
    Task<TableReservationDto?> UpdateReservationAsync(Guid id, UpdateTableReservationDto updateDto);

    /// <summary>
    /// Confirms a reservation.
    /// </summary>
    Task<TableReservationDto?> ConfirmReservationAsync(Guid id);

    /// <summary>
    /// Marks a reservation as arrived.
    /// </summary>
    Task<TableReservationDto?> MarkArrivedAsync(Guid id);

    /// <summary>
    /// Cancels a reservation.
    /// </summary>
    Task<bool> CancelReservationAsync(Guid id);

    /// <summary>
    /// Marks a reservation as no-show.
    /// </summary>
    Task<TableReservationDto?> MarkNoShowAsync(Guid id);
}
