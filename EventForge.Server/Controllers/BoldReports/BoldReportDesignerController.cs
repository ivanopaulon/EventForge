using BoldReports.Web;
using BoldReports.Web.ReportDesigner;
using BoldReports.Web.ReportViewer;
using EventForge.Server.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Prym.DTOs.Reports;

namespace EventForge.Server.Controllers.BoldReports;

/// <summary>
/// Bold Reports designer service endpoint.
/// The JavaScript ReportDesigner component communicates with this controller via the ServiceUrl property.
/// Configure the JavaScript designer with: <c>serviceUrl: '/api/v1/boldreports/designer'</c>.
///
/// Implements <see cref="IReportDesignerController"/> (which extends <see cref="IReportController"/>)
/// as required by BoldReports.Net.Core.
/// </summary>
[Route("api/v1/boldreports/designer")]
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
public class BoldReportDesignerController(
    IReportDefinitionService reportService,
    IMemoryCache memoryCache,
    ILogger<BoldReportDesignerController> logger) : Microsoft.AspNetCore.Mvc.Controller, IReportDesignerController
{
    // ── IReportDesignerController implementation ─────────────────────────────

    /// <summary>
    /// Main POST action — processes all designer interactions (load, save, data sets, etc.).
    /// Called by the Bold Reports JavaScript ReportDesigner on every interaction.
    /// </summary>
    [HttpPost]
    public object PostDesignerAction([FromBody] Dictionary<string, object> jsonResult)
    {
        try
        {
            return ReportDesignerHelper.ProcessDesigner(jsonResult, this, null, memoryCache);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Bold Reports designer action");
            throw;
        }
    }

    /// <summary>
    /// POST form action for multipart form data (images, external data source imports, etc.).
    /// </summary>
    [HttpPost("form")]
    [ActionName("PostFormDesignerAction")]
    public object PostFormDesignerAction()
    {
        try
        {
            var formDict = Request.Form.Keys
                .ToDictionary(k => k, k => (object)Request.Form[k].ToString());
            var uploadedFile = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;
            return ReportDesignerHelper.ProcessDesigner(formDict, this, uploadedFile, memoryCache);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Bold Reports designer form action");
            throw;
        }
    }

    /// <summary>
    /// Upload action for report files and images. Returns void as required by the interface.
    /// </summary>
    [HttpPost("upload")]
    [ActionName("UploadReportAction")]
    public void UploadReportAction()
    {
        try
        {
            var formDict = Request.Form.Keys
                .ToDictionary(k => k, k => (object)Request.Form[k].ToString());
            var uploadedFile = Request.Form.Files.Count > 0 ? Request.Form.Files[0] : null;
            ReportDesignerHelper.ProcessDesigner(formDict, this, uploadedFile, memoryCache);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Bold Reports upload action");
            throw;
        }
    }

    /// <summary>
    /// Returns a report image by key (used by the designer preview panel).
    /// </summary>
    public object GetImage(string key, string image)
    {
        return ReportDesignerHelper.GetImage(key, image, this);
    }

    /// <summary>
    /// Reads a resource (report file, shared data set, etc.) from storage.
    /// The Bold Reports designer calls this to retrieve previously saved resources.
    /// </summary>
    public ResourceInfo GetData(string key, string resourceType)
    {
        // The key is the report GUID when the designer loads an existing report.
        if (Guid.TryParse(key, out var reportId))
        {
            try
            {
                var report = reportService.GetReportAsync(reportId).GetAwaiter().GetResult();
                if (report?.ReportContent is { Length: > 0 })
                {
                    return new ResourceInfo
                    {
                        Data = System.Text.Encoding.UTF8.GetBytes(report.ReportContent),
                    };
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading RDLC content for report {ReportId}", reportId);
                return new ResourceInfo { ErrorMessage = "Failed to load report definition." };
            }
        }

        return new ResourceInfo();
    }

    /// <summary>
    /// Writes a resource to storage. Report RDLC saving is delegated to PUT /api/v1/reports/{id}.
    /// </summary>
    public bool SetData(string key, string resourceType, ItemInfo itemContent, out string errMsg)
    {
        errMsg = string.Empty;
        return true;
    }

    // ── IReportController (required by IReportDesignerController) ────────────

    /// <summary>
    /// Viewer POST action — supports the preview panel embedded in the designer.
    /// </summary>
    public object PostReportAction(Dictionary<string, object> jsonResult)
    {
        try
        {
            return ReportHelper.ProcessReport(jsonResult, this, memoryCache);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Bold Reports preview action in designer");
            throw;
        }
    }

    /// <summary>
    /// Viewer POST form action for the embedded preview panel.
    /// </summary>
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
            logger.LogError(ex, "Error processing Bold Reports preview form action in designer");
            throw;
        }
    }

    /// <summary>
    /// GET resource endpoint — provides images, fonts, and other static resources.
    /// </summary>
    [HttpGet]
    [ActionName("GetResource")]
    public object GetResource([FromQuery] ReportResource resource)
    {
        return ReportHelper.GetResource(resource, this, memoryCache);
    }

    /// <summary>
    /// Callback invoked before report options are initialised for the embedded viewer.
    /// </summary>
    public void OnInitReportOptions(ReportViewerOptions reportViewerOptions)
    {
        // No customisation required for the designer preview.
    }

    /// <summary>
    /// Callback invoked after the report has been loaded in the embedded viewer.
    /// </summary>
    public void OnReportLoaded(ReportViewerOptions reportViewerOptions)
    {
        // No post-load customisation required.
    }
}
