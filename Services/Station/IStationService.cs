using EventForge.DTOs.Station;

namespace EventForge.Services.Station;

/// <summary>
/// Service interface for managing stations and printers.
/// </summary>
public interface IStationService
{
    // Station CRUD operations

    /// <summary>
    /// Gets all stations with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of stations</returns>
    Task<PagedResult<StationDto>> GetStationsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a station by ID.
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Station DTO or null if not found</returns>
    Task<StationDto?> GetStationByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new station.
    /// </summary>
    /// <param name="createStationDto">Station creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created station DTO</returns>
    Task<StationDto> CreateStationAsync(CreateStationDto createStationDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing station.
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="updateStationDto">Station update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated station DTO or null if not found</returns>
    Task<StationDto?> UpdateStationAsync(Guid id, UpdateStationDto updateStationDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a station (soft delete).
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteStationAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Printer CRUD operations

    /// <summary>
    /// Gets all printers with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of printers</returns>
    Task<PagedResult<PrinterDto>> GetPrintersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a printer by ID.
    /// </summary>
    /// <param name="id">Printer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Printer DTO or null if not found</returns>
    Task<PrinterDto?> GetPrinterByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets printers by station.
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of printers for the station</returns>
    Task<IEnumerable<PrinterDto>> GetPrintersByStationAsync(Guid stationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new printer.
    /// </summary>
    /// <param name="createPrinterDto">Printer creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created printer DTO</returns>
    Task<PrinterDto> CreatePrinterAsync(CreatePrinterDto createPrinterDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing printer.
    /// </summary>
    /// <param name="id">Printer ID</param>
    /// <param name="updatePrinterDto">Printer update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated printer DTO or null if not found</returns>
    Task<PrinterDto?> UpdatePrinterAsync(Guid id, UpdatePrinterDto updatePrinterDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a printer (soft delete).
    /// </summary>
    /// <param name="id">Printer ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeletePrinterAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a station exists.
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> StationExistsAsync(Guid stationId, CancellationToken cancellationToken = default);
}