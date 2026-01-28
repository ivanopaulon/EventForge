using EventForge.DTOs.Banks;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.VatRates;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Banks;
using EventForge.Server.Services.Business;
using EventForge.Server.Services.VatRates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace EventForge.Server.Controllers;

/// <summary>
/// Consolidated REST API controller for financial entity management (Banks, Payment Terms, VAT Rates).
/// Provides unified CRUD operations with multi-tenant support and standardized patterns.
/// This controller consolidates BanksController, PaymentTermsController, and VatRatesController
/// to reduce endpoint fragmentation and improve maintainability.
/// </summary>
[Route("api/v1/financial")]
[Authorize]
public class FinancialManagementController : BaseApiController
{
    private readonly IBankService _bankService;
    private readonly IPaymentTermService _paymentTermService;
    private readonly IVatRateService _vatRateService;
    private readonly IVatNatureService _vatNatureService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FinancialManagementController> _logger;

    public FinancialManagementController(
        IBankService bankService,
        IPaymentTermService paymentTermService,
        IVatRateService vatRateService,
        IVatNatureService vatNatureService,
        ITenantContext tenantContext,
        ILogger<FinancialManagementController> logger)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _paymentTermService = paymentTermService ?? throw new ArgumentNullException(nameof(paymentTermService));
        _vatRateService = vatRateService ?? throw new ArgumentNullException(nameof(vatRateService));
        _vatNatureService = vatNatureService ?? throw new ArgumentNullException(nameof(vatNatureService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Bank Management

    /// <summary>
    /// Retrieves all banks with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of banks</returns>
    /// <response code="200">Successfully retrieved banks with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("banks")]
    [ProducesResponseType(typeof(PagedResult<BankDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<BankDto>>> GetBanks(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _bankService.GetBanksAsync(pagination, cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
            
            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving banks.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving banks.", ex);
        }
    }

    /// <summary>
    /// Gets a bank by ID.
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bank information</returns>
    /// <response code="200">Returns the bank</response>
    /// <response code="404">If the bank is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("banks/{id:guid}")]
    [ProducesResponseType(typeof(BankDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BankDto>> GetBank(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var bank = await _bankService.GetBankByIdAsync(id, cancellationToken);
            if (bank == null)
            {
                return CreateNotFoundProblem($"Bank with ID {id} not found.");
            }

            return Ok(bank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the bank.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the bank.", ex);
        }
    }

    /// <summary>
    /// Creates a new bank.
    /// </summary>
    /// <param name="createBankDto">Bank creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created bank information</returns>
    /// <response code="201">Bank created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("banks")]
    [ProducesResponseType(typeof(BankDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BankDto>> CreateBank(
        [FromBody] CreateBankDto createBankDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _bankService.CreateBankAsync(createBankDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetBank), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the bank.");
            return CreateInternalServerErrorProblem("An error occurred while creating the bank.", ex);
        }
    }

    /// <summary>
    /// Updates an existing bank.
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="updateBankDto">Bank update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated bank information</returns>
    /// <response code="200">Bank updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the bank is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("banks/{id:guid}")]
    [ProducesResponseType(typeof(BankDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BankDto>> UpdateBank(
        Guid id,
        [FromBody] UpdateBankDto updateBankDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _bankService.UpdateBankAsync(id, updateBankDto, GetCurrentUser(), cancellationToken);
            if (result == null)
            {
                return CreateNotFoundProblem($"Bank with ID {id} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the bank.");
            return CreateInternalServerErrorProblem("An error occurred while updating the bank.", ex);
        }
    }

    /// <summary>
    /// Deletes a bank.
    /// </summary>
    /// <param name="id">Bank ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Bank deleted successfully</response>
    /// <response code="404">If the bank is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("banks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteBank(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _bankService.DeleteBankAsync(id, GetCurrentUser(), cancellationToken);
            if (!result)
            {
                return CreateNotFoundProblem($"Bank with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the bank.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the bank.", ex);
        }
    }

    #endregion

    #region Payment Terms Management

    /// <summary>
    /// Retrieves all payment terms with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of payment terms</returns>
    /// <response code="200">Successfully retrieved payment terms with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("payment-terms")]
    [ProducesResponseType(typeof(PagedResult<PaymentTermDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PaymentTermDto>>> GetPaymentTerms(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _paymentTermService.GetPaymentTermsAsync(pagination, cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
            
            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving payment terms.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving payment terms.", ex);
        }
    }

    /// <summary>
    /// Gets a payment term by ID.
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment term information</returns>
    /// <response code="200">Returns the payment term</response>
    /// <response code="404">If the payment term is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("payment-terms/{id:guid}")]
    [ProducesResponseType(typeof(PaymentTermDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentTermDto>> GetPaymentTerm(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var paymentTerm = await _paymentTermService.GetPaymentTermByIdAsync(id, cancellationToken);
            if (paymentTerm == null)
            {
                return CreateNotFoundProblem($"Payment term with ID {id} not found.");
            }

            return Ok(paymentTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the payment term.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the payment term.", ex);
        }
    }

    /// <summary>
    /// Creates a new payment term.
    /// </summary>
    /// <param name="createPaymentTermDto">Payment term creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment term information</returns>
    /// <response code="201">Payment term created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("payment-terms")]
    [ProducesResponseType(typeof(PaymentTermDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentTermDto>> CreatePaymentTerm(
        [FromBody] CreatePaymentTermDto createPaymentTermDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _paymentTermService.CreatePaymentTermAsync(createPaymentTermDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetPaymentTerm), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the payment term.");
            return CreateInternalServerErrorProblem("An error occurred while creating the payment term.", ex);
        }
    }

    /// <summary>
    /// Updates an existing payment term.
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="updatePaymentTermDto">Payment term update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment term information</returns>
    /// <response code="200">Payment term updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the payment term is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("payment-terms/{id:guid}")]
    [ProducesResponseType(typeof(PaymentTermDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentTermDto>> UpdatePaymentTerm(
        Guid id,
        [FromBody] UpdatePaymentTermDto updatePaymentTermDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _paymentTermService.UpdatePaymentTermAsync(id, updatePaymentTermDto, GetCurrentUser(), cancellationToken);
            if (result == null)
            {
                return CreateNotFoundProblem($"Payment term with ID {id} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the payment term.");
            return CreateInternalServerErrorProblem("An error occurred while updating the payment term.", ex);
        }
    }

    /// <summary>
    /// Deletes a payment term.
    /// </summary>
    /// <param name="id">Payment term ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Payment term deleted successfully</response>
    /// <response code="404">If the payment term is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("payment-terms/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeletePaymentTerm(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _paymentTermService.DeletePaymentTermAsync(id, GetCurrentUser(), cancellationToken);
            if (!result)
            {
                return CreateNotFoundProblem($"Payment term with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the payment term.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the payment term.", ex);
        }
    }

    #endregion

    #region VAT Rates Management

    /// <summary>
    /// Retrieves all VAT rates with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of VAT rates</returns>
    /// <response code="200">Successfully retrieved VAT rates with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("vat-rates")]
    [ProducesResponseType(typeof(PagedResult<VatRateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<VatRateDto>>> GetVatRates(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _vatRateService.GetVatRatesAsync(pagination, cancellationToken);
            
            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());
            
            if (pagination.WasCapped)
            {
                Response.Headers.Append("X-Pagination-Capped", "true");
                Response.Headers.Append("X-Pagination-Applied-Max", pagination.AppliedMaxPageSize.ToString());
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving VAT rates.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving VAT rates.", ex);
        }
    }

    /// <summary>
    /// Gets a VAT rate by ID.
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT rate information</returns>
    /// <response code="200">Returns the VAT rate</response>
    /// <response code="404">If the VAT rate is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("vat-rates/{id:guid}")]
    [ProducesResponseType(typeof(VatRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VatRateDto>> GetVatRate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var vatRate = await _vatRateService.GetVatRateByIdAsync(id, cancellationToken);
            if (vatRate == null)
            {
                return CreateNotFoundProblem($"VAT rate with ID {id} not found.");
            }

            return Ok(vatRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the VAT rate.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the VAT rate.", ex);
        }
    }

    /// <summary>
    /// Creates a new VAT rate.
    /// </summary>
    /// <param name="createVatRateDto">VAT rate creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created VAT rate information</returns>
    /// <response code="201">VAT rate created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("vat-rates")]
    [ProducesResponseType(typeof(VatRateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VatRateDto>> CreateVatRate(
        [FromBody] CreateVatRateDto createVatRateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _vatRateService.CreateVatRateAsync(createVatRateDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetVatRate), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the VAT rate.");
            return CreateInternalServerErrorProblem("An error occurred while creating the VAT rate.", ex);
        }
    }

    /// <summary>
    /// Updates an existing VAT rate.
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="updateVatRateDto">VAT rate update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated VAT rate information</returns>
    /// <response code="200">VAT rate updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the VAT rate is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("vat-rates/{id:guid}")]
    [ProducesResponseType(typeof(VatRateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VatRateDto>> UpdateVatRate(
        Guid id,
        [FromBody] UpdateVatRateDto updateVatRateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _vatRateService.UpdateVatRateAsync(id, updateVatRateDto, GetCurrentUser(), cancellationToken);
            if (result == null)
            {
                return CreateNotFoundProblem($"VAT rate with ID {id} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the VAT rate.");
            return CreateInternalServerErrorProblem("An error occurred while updating the VAT rate.", ex);
        }
    }

    /// <summary>
    /// Deletes a VAT rate.
    /// </summary>
    /// <param name="id">VAT rate ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">VAT rate deleted successfully</response>
    /// <response code="404">If the VAT rate is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("vat-rates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteVatRate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _vatRateService.DeleteVatRateAsync(id, GetCurrentUser(), cancellationToken);
            if (!result)
            {
                return CreateNotFoundProblem($"VAT rate with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the VAT rate.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the VAT rate.", ex);
        }
    }

    #endregion

    #region VAT Natures Management

    /// <summary>
    /// Gets all VAT natures with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of VAT natures</returns>
    /// <response code="200">Returns the paginated list of VAT natures</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("vat-natures")]
    [ProducesResponseType(typeof(PagedResult<VatNatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<VatNatureDto>>> GetVatNatures(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var paginationError = ValidatePaginationParameters(page, pageSize);
        if (paginationError != null) return paginationError;

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _vatNatureService.GetVatNaturesAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving VAT natures.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving VAT natures.", ex);
        }
    }

    /// <summary>
    /// Gets a VAT nature by ID.
    /// </summary>
    /// <param name="id">VAT nature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>VAT nature information</returns>
    /// <response code="200">Returns the VAT nature</response>
    /// <response code="404">If the VAT nature is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("vat-natures/{id:guid}")]
    [ProducesResponseType(typeof(VatNatureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VatNatureDto>> GetVatNature(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var vatNature = await _vatNatureService.GetVatNatureByIdAsync(id, cancellationToken);
            if (vatNature == null)
            {
                return CreateNotFoundProblem($"VAT nature with ID {id} not found.");
            }

            return Ok(vatNature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the VAT nature.");
            return CreateInternalServerErrorProblem("An error occurred while retrieving the VAT nature.", ex);
        }
    }

    /// <summary>
    /// Creates a new VAT nature.
    /// </summary>
    /// <param name="createVatNatureDto">VAT nature creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created VAT nature information</returns>
    /// <response code="201">VAT nature created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("vat-natures")]
    [ProducesResponseType(typeof(VatNatureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VatNatureDto>> CreateVatNature(
        [FromBody] CreateVatNatureDto createVatNatureDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _vatNatureService.CreateVatNatureAsync(createVatNatureDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetVatNature), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating the VAT nature.");
            return CreateInternalServerErrorProblem("An error occurred while creating the VAT nature.", ex);
        }
    }

    /// <summary>
    /// Updates an existing VAT nature.
    /// </summary>
    /// <param name="id">VAT nature ID</param>
    /// <param name="updateVatNatureDto">VAT nature update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated VAT nature information</returns>
    /// <response code="200">VAT nature updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the VAT nature is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("vat-natures/{id:guid}")]
    [ProducesResponseType(typeof(VatNatureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VatNatureDto>> UpdateVatNature(
        Guid id,
        [FromBody] UpdateVatNatureDto updateVatNatureDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _vatNatureService.UpdateVatNatureAsync(id, updateVatNatureDto, GetCurrentUser(), cancellationToken);
            if (result == null)
            {
                return CreateNotFoundProblem($"VAT nature with ID {id} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the VAT nature.");
            return CreateInternalServerErrorProblem("An error occurred while updating the VAT nature.", ex);
        }
    }

    /// <summary>
    /// Deletes a VAT nature.
    /// </summary>
    /// <param name="id">VAT nature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">VAT nature deleted successfully</response>
    /// <response code="404">If the VAT nature is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("vat-natures/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteVatNature(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await _vatNatureService.DeleteVatNatureAsync(id, GetCurrentUser(), cancellationToken);
            if (!result)
            {
                return CreateNotFoundProblem($"VAT nature with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the VAT nature.");
            return CreateInternalServerErrorProblem("An error occurred while deleting the VAT nature.", ex);
        }
    }

    #endregion
}