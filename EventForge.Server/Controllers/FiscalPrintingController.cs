using EventForge.Server.Services.FiscalPrinting;
using EventForge.Server.Services.Station;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.FiscalPrinting;
using Prym.DTOs.Station;
using System.Text.Json;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for fiscal printer operations.
/// Supports receipt printing, refunds, daily closure, real-time status, cash drawer management,
/// network scanning, printer info reading, and wizard setup.
/// All operations are authorised for the <c>Admin</c>, <c>Manager</c>, and <c>StoreManager</c> roles.
/// </summary>
[Route("api/v1/fiscal-printing")]
[Authorize(Policy = "RequireStoreConfig")]
public partial class FiscalPrintingController(
    IFiscalPrinterService fiscalPrinterService,
    FiscalPrinterStatusCache statusCache,
    IStationService stationService,
    ITenantContext tenantContext,
    IAuditLogService auditLogService,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<FiscalPrintingController> logger) : BaseApiController
{
    // -------------------------------------------------------------------------
    //  Print receipt
    // -------------------------------------------------------------------------

}

/// <summary>Response DTO for the agent system-printers proxy endpoint.</summary>
public sealed record AgentSystemPrintersResponse(IReadOnlyList<string> Printers);
