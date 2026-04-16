using Prym.DTOs.Common;
using Prym.DTOs.Station;

namespace Prym.Web.Services.Station;

/// <summary>
/// Client service interface for managing stations.
/// </summary>
public interface IStationService
{
    /// <summary>Gets all stations.</summary>
    Task<List<StationDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Gets a station by ID.</summary>
    Task<StationDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Creates a new station.</summary>
    Task<StationDto?> CreateAsync(CreateStationDto createDto, CancellationToken ct = default);

    /// <summary>Updates an existing station.</summary>
    Task<StationDto?> UpdateAsync(Guid id, UpdateStationDto updateDto, CancellationToken ct = default);

    /// <summary>Deletes a station.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets stations with pagination.</summary>
    Task<PagedResult<StationDto>> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    // ── Printer endpoints ─────────────────────────────────────

    /// <summary>Gets all printers.</summary>
    Task<List<PrinterDto>> GetAllPrintersAsync(CancellationToken ct = default);

    /// <summary>Gets a printer by ID.</summary>
    Task<PrinterDto?> GetPrinterByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets printers with pagination and optional search.</summary>
    Task<PagedResult<PrinterDto>> GetPagedPrintersAsync(int page = 1, int pageSize = 20, string? searchTerm = null, CancellationToken ct = default);

    /// <summary>Creates a new printer.</summary>
    Task<PrinterDto?> CreatePrinterAsync(CreatePrinterDto createDto, CancellationToken ct = default);

    /// <summary>Updates an existing printer.</summary>
    Task<PrinterDto?> UpdatePrinterAsync(Guid id, UpdatePrinterDto updateDto, CancellationToken ct = default);

    /// <summary>Deletes a printer.</summary>
    Task<bool> DeletePrinterAsync(Guid id, CancellationToken ct = default);
}
