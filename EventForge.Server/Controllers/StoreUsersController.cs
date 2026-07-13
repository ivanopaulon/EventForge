using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Store;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for store user management with multi-tenant support.
/// Provides CRUD operations for store users within the authenticated user's tenant context.
/// Read operations are available to all authenticated users (for POS operator selection).
/// Write operations require Manager role.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public partial class StoreUsersController(IStoreUserService storeUserService, ITenantContext tenantContext) : BaseApiController
{

}