using BoldReports.Web;
using BoldReports.Web.ReportViewer;
using EventForge.Server.Helpers;
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
            var normalised = BoldReportsJsonHelper.NormaliseJsonElements(jsonResult);
            return ReportHelper.ProcessReport(normalised, this, memoryCache);
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
    /// Resolves the GUID-based report path to the stored RDLC content and
    /// provides it as a <see cref="Stream"/> so Bold Reports uses it directly.
    /// </summary>
    [NonAction]
    public void OnInitReportOptions(ReportViewerOptions reportViewerOptions)
    {
        var reportPath = reportViewerOptions.ReportModel.ReportPath;
        if (!Guid.TryParse(reportPath, out var reportId))
            return;

        try
        {
            var report = Task.Run(() => reportService.GetReportAsync(reportId)).GetAwaiter().GetResult();
            if (report?.ReportContent is { Length: > 0 })
            {
                var rdlcBytes = System.Text.Encoding.UTF8.GetBytes(report.ReportContent);
                reportViewerOptions.ReportModel.Stream = new System.IO.MemoryStream(rdlcBytes);
                reportViewerOptions.ReportModel.ReportPath = null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading RDLC content for report {ReportId}", reportId);
        }
    }

    /// <summary>
    /// Callback invoked after the report has been loaded by the Bold Reports engine.
    /// </summary>
    [NonAction]
    public void OnReportLoaded(ReportViewerOptions reportViewerOptions)
    {
        // No post-load customisation required.
    }
}
