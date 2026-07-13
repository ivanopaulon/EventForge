using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.PriceLists;
using Prym.DTOs.Products;
using Prym.DTOs.Promotions;
using Prym.DTOs.UnitOfMeasures;
using Prym.DTOs.Warehouse;

namespace EventForge.Server.Controllers;

/// <summary>
/// Consolidated REST API controller for product management with multi-tenant support.
/// Provides unified CRUD operations for products, units of measure, price lists, promotions, and barcodes
/// within the authenticated user's tenant context.
/// This controller consolidates ProductsController, UnitOfMeasuresController, PriceListsController, 
/// PromotionsController, and BarcodeController to reduce endpoint fragmentation and improve maintainability.
/// </summary>
[Route("api/v1/product-management")]
[Authorize]
[RequireLicenseFeature("ProductManagement")]
public partial class ProductManagementController(
    IProductService productService,
    IBrandService brandService,
    IModelService modelService,
    IUMService umService,
    IPriceListService priceListService,
    IPriceListGenerationService priceListGenerationService,
    IPriceCalculationService priceCalculationService,
    IPriceListBusinessPartyService priceListBusinessPartyService,
    IPromotionService promotionService,
    IBarcodeService barcodeService,
    IDocumentHeaderService documentHeaderService,
    IStockMovementService stockMovementService,
    ITenantContext tenantContext,
    ILogger<ProductManagementController> logger,
    IExportService exportService) : BaseApiController
{

}

/// <summary>
/// DTO for image upload result.
/// </summary>
public class ImageUploadResultDto
{
    /// <summary>
    /// URL of the uploaded image.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;
}