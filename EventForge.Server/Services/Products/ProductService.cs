using EventForge.Server.Services.CodeGeneration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Prym.DTOs.Products;
using EntityProductCodeStatus = EventForge.Server.Data.Entities.Products.ProductCodeStatus;
using EntityProductStatus = EventForge.Server.Data.Entities.Products.ProductStatus;
using EntityProductUnitStatus = EventForge.Server.Data.Entities.Products.ProductUnitStatus;

namespace EventForge.Server.Services.Products;

/// <summary>
/// Service implementation for managing products and related entities.
/// </summary>
public partial class ProductService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<ProductService> logger,
    IDailyCodeGenerator codeGenerator) : IProductService
{

    // Default currency for product transactions
    private const string DefaultCurrency = "EUR";

    // Maximum retry attempts for unique constraint violations
    private const int MaxRetryAttempts = 3;

    // Request-scoped cache for classification nodes (avoids redundant DB round-trips within a single request)
    private List<(Guid Id, Guid? ParentId)>? _classificationNodesCache;

    // Product CRUD operations

}
