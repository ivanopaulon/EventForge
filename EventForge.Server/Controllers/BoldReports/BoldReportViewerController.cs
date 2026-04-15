using BoldReports.Web;
using BoldReports.Web.ReportViewer;
using EventForge.Server.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace EventForge.Server.Controllers.BoldReports;

/// <summary>
/// Bold Reports viewer service endpoint.
/// The JavaScript ReportViewer component communicates with this controller via the ServiceUrl property.
/// Configure the JavaScript viewer with: <c>serviceUrl: '/api/v1/boldreports/viewer'</c>.
///
/// Implements <see cref="IReportController"/> as required by BoldReports.Net.Core.
/// </summary>
[Route("api/v1/boldreports/viewer")]
[ApiController]
[Authorize]
public class BoldReportViewerController(
    IReportDefinitionService reportService,
    IMemoryCache memoryCache,
    ILogger<BoldReportViewerController> logger) : Microsoft.AspNetCore.Mvc.Controller, IReportController
{
    // ── IReportController implementation ─────────────────────────────────────

    /// <summary>
    /// Main POST action — processes all viewer interactions (paging, exporting, parameter changes).
    /// Called by the Bold Reports JavaScript ReportViewer on every user interaction.
    /// </summary>
    [HttpPost]
    public object PostReportAction([FromBody] Dictionary<string, object> jsonResult)
    {
        try
        {
            return ReportHelper.ProcessReport(jsonResult, this, memoryCache);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Bold Reports viewer action");
            throw;
        }
    }

    /// <summary>
    /// POST form action for multipart form data (used for some export operations).
    /// </summary>
    [HttpPost("form")]
    [ActionName("PostFormReportAction")]
    public object PostFormReportAction()
    {
        try
        {
            var formDict = Request.Form.Keys
                .ToDictionary(k => k, k => (object)Request.Form[k].ToString());
            return ReportHelper.ProcessReport(formDict, this, memoryCache);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Bold Reports form action");
            throw;
        }
    }

    /// <summary>
    /// GET resource endpoint — provides images, fonts, and other static viewer resources.
    /// </summary>
    [HttpGet]
    [ActionName("GetResource")]
    public object GetResource([FromQuery] ReportResource resource)
    {
        return ReportHelper.GetResource(resource, this, memoryCache);
    }

    /// <summary>
    /// Callback invoked before report options are initialised.
    /// Override to customise the report path or parameters before loading.
    /// </summary>
    public void OnInitReportOptions(ReportViewerOptions reportViewerOptions)
    {
        // Resolve GUID-based report path to the stored RDLC content via the ReportHelper pipeline.
    }

    /// <summary>
    /// Callback invoked after the report has been loaded by the Bold Reports engine.
    /// </summary>
    public void OnReportLoaded(ReportViewerOptions reportViewerOptions)
    {
        // No post-load customisation required.
    }
}
