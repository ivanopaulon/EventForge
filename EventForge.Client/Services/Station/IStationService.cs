using EventForge.DTOs.Common;
using EventForge.DTOs.Station;

namespace EventForge.Client.Services.Station;

/// <summary>
/// Client service interface for managing stations.
/// </summary>
public interface IStationService
{
    /// <summary>Gets all stations.</summary>
    Task<List<StationDto>> GetAllAsync();

    /// <summary>Gets a station by ID.</summary>
    Task<StationDto?> GetByIdAsync(Guid id);

    /// <summary>Creates a new station.</summary>
    Task<StationDto?> CreateAsync(CreateStationDto createDto);

    /// <summary>Updates an existing station.</summary>
    Task<StationDto?> UpdateAsync(Guid id, UpdateStationDto updateDto);

    /// <summary>Deletes a station.</summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>Gets stations with pagination.</summary>
    Task<PagedResult<StationDto>> GetPagedAsync(int page = 1, int pageSize = 20);

    // ── Printer endpoints ─────────────────────────────────────

    /// <summary>Gets all printers.</summary>
    Task<List<PrinterDto>> GetAllPrintersAsync();

    /// <summary>Gets a printer by ID.</summary>
    Task<PrinterDto?> GetPrinterByIdAsync(Guid id);

    /// <summary>Creates a new printer.</summary>
    Task<PrinterDto?> CreatePrinterAsync(CreatePrinterDto createDto);

    /// <summary>Updates an existing printer.</summary>
    Task<PrinterDto?> UpdatePrinterAsync(Guid id, UpdatePrinterDto updateDto);
}
