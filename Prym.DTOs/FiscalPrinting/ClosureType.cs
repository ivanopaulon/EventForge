namespace Prym.DTOs.FiscalPrinting;

/// <summary>
/// Indicates how the daily closure was (or will be) executed.
/// </summary>
public enum ClosureType
{
    /// <summary>
    /// Standard fiscal closure: the Z-report command was sent to the fiscal printer hardware
    /// and the result was recorded in the database. This is the normal flow.
    /// </summary>
    Fiscale,

    /// <summary>
    /// Non-fiscal closure: the POS terminal has a non-fiscal thermal printer configured
    /// (or no printer at all). Day totals were computed from the database and a summary
    /// receipt was printed on the thermal printer (or not printed at all).
    /// No fiscal hardware was involved.
    /// </summary>
    NonFiscale,

    /// <summary>
    /// Database-only closure: the closure was recorded only in the EventForge database.
    /// This occurs when the fiscal printer is configured but was unreachable at the time
    /// of closure (see <see cref="Prym.DTOs.FiscalPrinting.DailyClosureResultDto.FiscalClosurePending"/>),
    /// or when the operator explicitly chose to proceed without printing.
    /// </summary>
    SoloDatabase
}
