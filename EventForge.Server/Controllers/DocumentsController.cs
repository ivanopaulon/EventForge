using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;

namespace EventForge.Server.Controllers;

/// <summary>
/// Unified REST API controller for document-related operations with multi-tenant support.
/// Provides aggregated access to document attachments, comments, templates, workflows, and analytics.
/// Delegates business logic to existing specialized services through the DocumentFacade.
/// </summary>
[Route("api/v1/documents")]
[Authorize]
[RequireLicenseFeature("BasicReporting")]
public partial class DocumentsController(
    IDocumentFacade documentFacade,
    ITenantContext tenantContext,
    ILogger<DocumentsController> logger) : BaseApiController
{

    // Attachment endpoints

}