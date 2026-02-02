using EventForge.Client.Services;
using EventForge.Client.Services.Sales;
using EventForge.Client.Services.Store;
using EventForge.DTOs.Business;
using EventForge.DTOs.Constants;
using EventForge.DTOs.Products;
using EventForge.DTOs.Sales;
using EventForge.DTOs.Store;
using Timer = System.Timers.Timer;

namespace EventForge.Client.ViewModels;

/// <summary>
/// ViewModel for POS page following MVVM pattern.
/// Handles all business logic, state management, and API communication for the Point of Sale.
/// </summary>
public class POSViewModel : IDisposable
{
    private readonly ISalesService _salesService;
    private readonly IStoreUserService _storeUserService;
    private readonly IStorePosService _storePosService;
    private readonly IBusinessPartyService _businessPartyService;
    private readonly IPaymentMethodService _paymentMethodService;
    private readonly ILogger<POSViewModel> _logger;

    // Debounce timer for item updates
    private Timer? _updateDebounceTimer;
    private readonly HashSet<Guid> _pendingItemUpdates = new();
    private const int DebounceDelayMs = 500;

    // Thread-safe synchronization for updates
    private readonly SemaphoreSlim _updateSemaphore = new(1, 1);
    private CancellationTokenSource? _currentUpdateCts;

    // Optimistic update rollback - stores original values before modification
    private readonly Dictionary<Guid, SaleItemDto> _itemBackups = new();

    // Retry tracking
    private int _currentRetryAttempt = 0;
    private const int MaxRetryAttempts = 3;
    private const int ManualRetryAttempts = 2;

    // Timeouts
    private const int FlushTimeoutMs = 5000;

    // Page size for loading operators
    private const int MaxOperatorsPageSize = 100;

    public POSViewModel(
        ISalesService salesService,
        IStoreUserService storeUserService,
        IStorePosService storePosService,
        IBusinessPartyService businessPartyService,
        IPaymentMethodService paymentMethodService,
        ILogger<POSViewModel> logger)
    {
        _salesService = salesService;
        _storeUserService = storeUserService;
        _storePosService = storePosService;
        _businessPartyService = businessPartyService;
        _paymentMethodService = paymentMethodService;
        _logger = logger;
    }

    #region State Properties

    // Loading states
    public bool IsLoading { get; private set; }
    public bool IsSaving { get; private set; }
    public bool IsUpdatingItems { get; private set; }
    public bool IsSessionClosed { get; private set; }
    public string? ErrorMessage { get; set; }

    // Operator & POS selection
    public List<StoreUserDto> AvailableOperators { get; private set; } = new();

    private Guid? _selectedOperatorId;
    public Guid? SelectedOperatorId
    {
        get => _selectedOperatorId;
        set
        {
            if (_selectedOperatorId != value)
            {
                _selectedOperatorId = value;
                _ = TryCreateSessionAsync();
                NotifyStateChanged();
            }
        }
    }

    public List<StorePosDto> AvailablePos { get; private set; } = new();

    private Guid? _selectedPosId;
    public Guid? SelectedPosId
    {
        get => _selectedPosId;
        set
        {
            if (_selectedPosId != value)
            {
                _selectedPosId = value;
                _ = TryCreateSessionAsync();
                NotifyStateChanged();
            }
        }
    }

    // Customer
    public BusinessPartyDto? SelectedCustomer { get; set; }

    // Session
    public SaleSessionDto? CurrentSession { get; private set; }

    // Payment Methods
    public List<PaymentMethodDto> PaymentMethods { get; private set; } = new();

    // Product Preview
    public ProductDto? LastScannedProduct { get; set; }
    public ProductCodeDto? LastScannedCode { get; set; }
    public ScanMode ScanMode { get; set; } = ScanMode.AddToCart;

    #endregion

    #region Computed Properties

    public bool HasActiveSession => CurrentSession != null;
    public bool CanPay => HasActiveSession && CurrentSession!.Items.Any() && !CurrentSession.IsFullyPaid;
    public bool CanCloseSale => HasActiveSession && CurrentSession!.IsFullyPaid && !IsSessionClosed;
    public int ItemCount => CurrentSession?.Items?.Count ?? 0;
    public decimal GrandTotal => CurrentSession?.FinalTotal ?? 0m;
    public decimal RemainingAmount => CurrentSession?.RemainingAmount ?? 0m;

    #endregion

    #region Events

    public event Action? StateChanged;
    public event Action<string, bool>? OnNotification; // (message, isSuccess)

    private void NotifyStateChanged() => StateChanged?.Invoke();
    private void NotifySuccess(string message) => OnNotification?.Invoke(message, true);
    private void NotifyError(string message) => OnNotification?.Invoke(message, false);

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the ViewModel by loading operators, POS terminals, payment methods,
    /// and checking for suspended sessions.
    /// </summary>
    public async Task InitializeAsync(string? currentUsername = null)
    {
        try
        {
            IsLoading = true;
            NotifyStateChanged();

            _logger.LogInformation("POS ViewModel initialization started");

            // Load operators
            try
            {
                var pagedResult = await _storeUserService.GetPagedAsync(1, MaxOperatorsPageSize);
                AvailableOperators = pagedResult?.Items?.ToList() ?? new List<StoreUserDto>();
                _logger.LogInformation("Loaded {Count} operators", AvailableOperators.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading operators");
                NotifyError("Error loading operators");
                AvailableOperators = new List<StoreUserDto>();
            }

            // Load POS terminals
            try
            {
                var posResult = await _storePosService.GetActiveAsync();
                AvailablePos = posResult?.ToList() ?? new List<StorePosDto>();
                _logger.LogInformation("Loaded {Count} POS terminals", AvailablePos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading POS terminals");
                NotifyError("Error loading POS terminals");
                AvailablePos = new List<StorePosDto>();
            }

            // Load payment methods
            try
            {
                var paymentMethodsResult = await _paymentMethodService.GetActiveAsync();
                PaymentMethods = paymentMethodsResult?.ToList() ?? new List<PaymentMethodDto>();
                _logger.LogInformation("Loaded {Count} payment methods", PaymentMethods.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment methods");
                NotifyError("Error loading payment methods");
                PaymentMethods = new List<PaymentMethodDto>();
            }

            // Auto-select operator if user has matching StoreUser record
            if (!string.IsNullOrEmpty(currentUsername) && AvailableOperators.Count > 0)
            {
                var matchingOperator = AvailableOperators.FirstOrDefault(op =>
                    op.Username?.Equals(currentUsername, StringComparison.OrdinalIgnoreCase) == true);

                if (matchingOperator != null)
                {
                    _selectedOperatorId = matchingOperator.Id;
                    _logger.LogDebug("Auto-selected operator {OperatorName} for user {Username}",
                        matchingOperator.Name, currentUsername);
                }
                else
                {
                    _logger.LogWarning("No matching StoreUser found for username {Username}", currentUsername);
                }
            }

            // Auto-select POS if there is only one available
            if (_selectedOperatorId.HasValue && AvailablePos.Count == 1)
            {
                _selectedPosId = AvailablePos[0].Id;
                _logger.LogDebug("Auto-selected the only available POS: {PosName}", AvailablePos[0].Name);
            }

            // Try to create session if both are selected
            await TryCreateSessionAsync();

            // Show warnings if resources are missing
            if (AvailableOperators.Count == 0)
            {
                _logger.LogWarning("No store operators available");
                NotifyError("No operators available. Contact administrator.");
            }

            if (AvailablePos.Count == 0)
            {
                _logger.LogWarning("No active POS terminals available");
                NotifyError("No active POS terminals available. Contact administrator.");
            }

            // Check for suspended sessions
            await CheckForSuspendedSessionsAsync();

            _logger.LogInformation("POS ViewModel initialization complete");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(httpEx, "Unauthorized access during POS initialization");
            NotifyError("Unauthorized access. Please log in again.");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(httpEx, "Forbidden access during POS initialization");
            NotifyError("Access denied. Check tenant permissions and license.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing POS ViewModel");
            NotifyError($"Initialization error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Reloads the current session from the server to ensure UI reflects server state.
    /// </summary>
    public async Task<bool> ReloadSessionAsync()
    {
        if (CurrentSession == null)
        {
            _logger.LogWarning("Cannot reload session: CurrentSession is null");
            return false;
        }

        try
        {
            _logger.LogInformation("Reloading session {SessionId} from server", CurrentSession.Id);

            var reloadedSession = await _salesService.GetSessionAsync(CurrentSession.Id);

            if (reloadedSession != null)
            {
                CurrentSession = reloadedSession;
                NotifyStateChanged();
                _logger.LogInformation("Session reloaded: {ItemCount} items", CurrentSession.Items?.Count ?? 0);
                return true;
            }

            _logger.LogError("GetSessionAsync returned null");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload session");
            return false;
        }
    }

    /// <summary>
    /// Parks (suspends) the current session for later retrieval.
    /// </summary>
    public async Task<(bool Success, string? Error)> ParkSessionAsync()
    {
        if (CurrentSession == null)
            return (false, "No active session");

        try
        {
            // Flush pending updates before parking session
            await FlushPendingUpdatesAsync();

            var updateDto = new UpdateSaleSessionDto
            {
                Status = SaleSessionStatusDto.Suspended,
                CustomerId = SelectedCustomer?.Id
            };

            var updatedSession = await _salesService.UpdateSessionAsync(CurrentSession.Id, updateDto);
            if (updatedSession != null)
            {
                CurrentSession = updatedSession;
                NotifySuccess("Session parked");

                await PrepareNewSaleAsync();
                return (true, null);
            }

            _logger.LogError("UpdateSessionAsync returned null when parking session {SessionId}", CurrentSession.Id);
            return (false, "Error parking session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parking session");
            return (false, $"Error parking session: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels the current session.
    /// </summary>
    public async Task<(bool Success, string? Error)> CancelSessionAsync()
    {
        if (CurrentSession == null)
            return (false, "No active session");

        try
        {
            // Flush pending updates before canceling session
            await FlushPendingUpdatesAsync();

            var updateDto = new UpdateSaleSessionDto
            {
                Status = SaleSessionStatusDto.Cancelled
            };

            var updatedSession = await _salesService.UpdateSessionAsync(CurrentSession.Id, updateDto);

            if (updatedSession != null)
            {
                NotifySuccess("Session cancelled");
                await PrepareNewSaleAsync();
                return (true, null);
            }

            _logger.LogError("UpdateSessionAsync returned null when cancelling session {SessionId}", CurrentSession.Id);
            return (false, "Error cancelling session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling session");
            return (false, $"Error cancelling session: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a product to the current session.
    /// </summary>
    public async Task<(bool Success, string? Error)> AddProductAsync(ProductDto product)
    {
        if (CurrentSession == null || product == null)
            return (false, "No active session or invalid product");

        try
        {
            var existingItem = CurrentSession.Items.FirstOrDefault(i => i.ProductId == product.Id);

            if (existingItem != null)
            {
                // Increase quantity of existing item
                existingItem.Quantity++;
                await UpdateItemInternalAsync(existingItem);
                NotifySuccess($"Quantity increased: {product.Name}");
                return (true, null);
            }
            else
            {
                // Add new item - FLUSH and LOCK before calling API
                await FlushPendingUpdatesAsync();

                if (!await _updateSemaphore.WaitAsync(FlushTimeoutMs))
                {
                    _logger.LogWarning("Timeout waiting for update lock in AddProductAsync");
                    return (false, "Timeout acquiring lock");
                }

                try
                {
                    var addItemDto = new AddSaleItemDto
                    {
                        ProductId = product.Id,
                        Quantity = 1,
                        UnitPrice = product.DefaultPrice ?? 0m,
                        DiscountPercent = 0
                    };

                    var updatedSession = await _salesService.AddItemAsync(CurrentSession.Id, addItemDto);

                    if (updatedSession != null)
                    {
                        CurrentSession = updatedSession;
                        NotifyStateChanged();
                        NotifySuccess($"Product added: {product.Name}");
                        return (true, null);
                    }
                    else
                    {
                        _logger.LogWarning("AddItemAsync returned null, reloading session");
                        await ReloadSessionAsync();
                        return (false, "Error adding product");
                    }
                }
                finally
                {
                    _updateSemaphore.Release();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product");
            await ReloadSessionAsync();
            return (false, $"Error adding product: {ex.Message}");
        }
    }

    /// <summary>
    /// Queues an item update with debounce to batch rapid changes.
    /// Thread-safe with optimistic backup for rollback on errors.
    /// </summary>
    public void QueueItemUpdate(SaleItemDto item)
    {
        // Set loading state
        IsUpdatingItems = true;

        // Store backup of current item state for potential rollback
        if (!_itemBackups.ContainsKey(item.Id))
        {
            _itemBackups[item.Id] = new SaleItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                TotalAmount = item.TotalAmount,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                Notes = item.Notes,
                IsService = item.IsService,
                PromotionId = item.PromotionId,
                ProductThumbnailUrl = item.ProductThumbnailUrl,
                ProductImageUrl = item.ProductImageUrl,
                VatRateName = item.VatRateName,
                VatRateId = item.VatRateId,
                UnitOfMeasureName = item.UnitOfMeasureName,
                BrandName = item.BrandName
            };
        }

        // Add item ID to pending updates
        _pendingItemUpdates.Add(item.Id);

        // Initialize timer on first use or reuse existing one
        if (_updateDebounceTimer == null)
        {
            _updateDebounceTimer = new Timer(DebounceDelayMs);
            _updateDebounceTimer.AutoReset = false;

            // When timer fires, update all pending items
            _updateDebounceTimer.Elapsed += async (sender, e) =>
            {
                // Try to acquire semaphore - if already processing, this prevents double execution
                if (!await _updateSemaphore.WaitAsync(0))
                {
                    _logger.LogDebug("Skipping debounce execution - update already in progress");
                    return;
                }

                try
                {
                    // Cancel any previous update batch
                    _currentUpdateCts?.Cancel();
                    _currentUpdateCts?.Dispose();
                    _currentUpdateCts = new CancellationTokenSource();

                    var cancellationToken = _currentUpdateCts.Token;

                    // Get list of items to update
                    var itemsToUpdate = _pendingItemUpdates.ToList();
                    _pendingItemUpdates.Clear();

                    // Build dictionary for O(1) lookup
                    var itemsDict = CurrentSession?.Items
                        .GroupBy(i => i.Id)
                        .ToDictionary(g => g.Key, g => g.First());

                    // Update each pending item
                    if (itemsDict != null)
                    {
                        foreach (var itemId in itemsToUpdate)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogDebug("Update batch cancelled");
                                break;
                            }

                            if (itemsDict.TryGetValue(itemId, out var itemToUpdate))
                            {
                                await UpdateItemWithRetryAsync(itemToUpdate, MaxRetryAttempts, cancellationToken);
                            }
                        }
                    }

                    // Clear loading state after update completes
                    IsUpdatingItems = false;
                    NotifyStateChanged();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Update batch was cancelled");
                    // Don't clear IsUpdatingItems - new batch is starting
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in debounced update");
                    NotifyError("Error during update");

                    // Clear loading state on error
                    IsUpdatingItems = false;
                    NotifyStateChanged();
                }
                finally
                {
                    _updateSemaphore.Release();
                }
            };
        }

        // Stop timer and restart it (resets the debounce window)
        _updateDebounceTimer.Stop();
        _updateDebounceTimer.Start();

        // Notify state change for responsive UI
        NotifyStateChanged();
    }

    /// <summary>
    /// Removes an item from the current session.
    /// </summary>
    public async Task<(bool Success, string? Error)> RemoveItemAsync(SaleItemDto item)
    {
        if (CurrentSession == null)
            return (false, "No active session");

        // Flush pending updates and acquire semaphore
        await FlushPendingUpdatesAsync();

        if (!await _updateSemaphore.WaitAsync(FlushTimeoutMs))
        {
            _logger.LogWarning("Timeout waiting for update lock in RemoveItemAsync");
            return (false, "Timeout acquiring lock");
        }

        try
        {
            var updatedSession = await _salesService.RemoveItemAsync(CurrentSession.Id, item.Id);

            if (updatedSession != null)
            {
                CurrentSession = updatedSession;
                NotifyStateChanged();
                NotifySuccess("Item removed");
                return (true, null);
            }
            else
            {
                _logger.LogWarning("RemoveItemAsync returned null, reloading");
                await ReloadSessionAsync();
                return (false, "Error removing item");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item");
            await ReloadSessionAsync();
            return (false, $"Error removing item: {ex.Message}");
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    /// <summary>
    /// Adds a payment to the current session.
    /// </summary>
    public async Task<(bool Success, string? Error)> AddPaymentAsync(PaymentMethodDto method, decimal amount)
    {
        if (CurrentSession == null)
            return (false, "No active session");

        // Flush pending updates and acquire semaphore
        await FlushPendingUpdatesAsync();

        if (!await _updateSemaphore.WaitAsync(FlushTimeoutMs))
        {
            _logger.LogWarning("Timeout waiting for update lock in AddPaymentAsync");
            return (false, "Timeout acquiring lock");
        }

        try
        {
            var addPaymentDto = new AddSalePaymentDto
            {
                PaymentMethodId = method.Id,
                Amount = amount,
                Notes = null
            };

            var updatedSession = await _salesService.AddPaymentAsync(CurrentSession.Id, addPaymentDto);
            if (updatedSession != null)
            {
                CurrentSession = updatedSession;
                NotifyStateChanged();
                return (true, null);
            }
            else
            {
                _logger.LogError("AddPaymentAsync returned null for session {SessionId}", CurrentSession.Id);
                return (false, "Error adding payment");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment for method {MethodName}", method.Name);
            return (false, $"Error adding payment: {ex.Message}");
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    /// <summary>
    /// Closes the current sale session.
    /// </summary>
    public async Task<(bool Success, string? Error)> CloseSaleAsync()
    {
        if (CurrentSession == null || !CurrentSession.IsFullyPaid)
            return (false, "Session not ready to close");

        try
        {
            IsLoading = true;
            NotifyStateChanged();

            // Flush pending updates before closing session
            await FlushPendingUpdatesAsync();

            var updatedSession = await _salesService.CloseSessionAsync(CurrentSession.Id);
            if (updatedSession != null)
            {
                CurrentSession = updatedSession;
                IsSessionClosed = true;
                NotifySuccess("Sale completed successfully!");

                // Auto-prepare for new sale after a short delay
                await Task.Delay(2000);
                await PrepareNewSaleAsync();
                return (true, null);
            }
            else
            {
                _logger.LogError("CloseSessionAsync returned null for session {SessionId}", CurrentSession.Id);
                return (false, "Error closing sale");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing sale");
            return (false, $"Error closing sale: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Clears the product preview.
    /// </summary>
    public void ClearPreview()
    {
        LastScannedProduct = null;
        LastScannedCode = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Searches for customers by search term.
    /// </summary>
    public async Task<IEnumerable<BusinessPartyDto>> SearchCustomersAsync(string searchTerm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<BusinessPartyDto>();

        try
        {
            var customers = await _businessPartyService.SearchBusinessPartiesAsync(searchTerm, null, 50);
            return customers ?? Enumerable.Empty<BusinessPartyDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            return Enumerable.Empty<BusinessPartyDto>();
        }
    }

    /// <summary>
    /// Updates the selected customer for the current session.
    /// </summary>
    public async Task UpdateSelectedCustomerAsync(BusinessPartyDto? customer)
    {
        SelectedCustomer = customer;

        // If session exists, update it with the new customer
        if (CurrentSession != null && customer != null)
        {
            try
            {
                var updateDto = new UpdateSaleSessionDto
                {
                    CustomerId = customer.Id
                };

                var updatedSession = await _salesService.UpdateSessionAsync(CurrentSession.Id, updateDto);
                if (updatedSession != null)
                {
                    CurrentSession = updatedSession;
                }
                else
                {
                    _logger.LogWarning("UpdateSessionAsync returned null for session {SessionId}", CurrentSession.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session with customer");
                NotifyError("Error updating customer");
            }
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Adds a categorized note to the current sale session.
    /// </summary>
    public async Task<SaleSessionDto?> AddSessionNoteAsync(AddSessionNoteDto noteDto)
    {
        if (CurrentSession == null)
        {
            _logger.LogWarning("Cannot add note: no active session");
            return null;
        }
        
        try
        {
            _logger.LogInformation("Adding note to session {SessionId}", CurrentSession.Id);
            
            var updatedSession = await _salesService.AddNoteAsync(CurrentSession.Id, noteDto);
            
            if (updatedSession != null)
            {
                CurrentSession = updatedSession;
                NotifyStateChanged();
                _logger.LogInformation("Note added successfully to session {SessionId}", CurrentSession.Id);
            }
            
            return updatedSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding note to session {SessionId}", CurrentSession.Id);
            return null;
        }
    }

    /// <summary>
    /// Applies a global discount percentage to all items in the current session.
    /// </summary>
    public async Task<SaleSessionDto?> ApplyGlobalDiscountAsync(ApplyGlobalDiscountDto discountDto)
    {
        if (CurrentSession == null)
        {
            _logger.LogWarning("Cannot apply discount: no active session");
            return null;
        }
        
        try
        {
            IsUpdatingItems = true;
            NotifyStateChanged();
            
            _logger.LogInformation("Applying {DiscountPercent}% global discount to session {SessionId}", 
                discountDto.DiscountPercent, CurrentSession.Id);
            
            var updatedSession = await _salesService.ApplyGlobalDiscountAsync(CurrentSession.Id, discountDto);
            
            if (updatedSession != null)
            {
                CurrentSession = updatedSession;
                NotifyStateChanged();
                _logger.LogInformation("Global discount applied successfully");
            }
            
            return updatedSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying global discount to session {SessionId}", CurrentSession.Id);
            return null;
        }
        finally
        {
            IsUpdatingItems = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Resumes a suspended session.
    /// </summary>
    public async Task<bool> ResumeSessionAsync(SaleSessionDto session)
    {
        try
        {
            _logger.LogInformation("Resuming suspended session {SessionId}", session.Id);
            CurrentSession = session;
            _selectedOperatorId = session.OperatorId;
            _selectedPosId = session.PosId;

            if (session.CustomerId.HasValue)
            {
                SelectedCustomer = await _businessPartyService.GetBusinessPartyAsync(session.CustomerId.Value);
            }
            else
            {
                SelectedCustomer = null;
            }

            NotifyStateChanged();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming session");
            return false;
        }
    }

    #endregion

    #region Internal Methods

    private async Task TryCreateSessionAsync()
    {
        // Create session when both operator and POS are selected and no session exists
        if (_selectedOperatorId.HasValue && _selectedPosId.HasValue && CurrentSession == null)
        {
            await CreateNewSessionAsync();
        }
    }

    private async Task CreateNewSessionAsync()
    {
        if (!_selectedPosId.HasValue || !_selectedOperatorId.HasValue)
        {
            _logger.LogWarning("Cannot create session: PosId={PosId}, OperatorId={OperatorId}",
                _selectedPosId, _selectedOperatorId);
            return;
        }

        try
        {
            IsLoading = true;
            NotifyStateChanged();

            _logger.LogInformation("Creating new sale session for POS {PosId}, Operator {OperatorId}",
                _selectedPosId.Value, _selectedOperatorId.Value);

            var createDto = new CreateSaleSessionDto
            {
                OperatorId = _selectedOperatorId.Value,
                PosId = _selectedPosId.Value,
                CustomerId = SelectedCustomer?.Id,
                SaleType = SaleTypes.Retail,
                Currency = Currencies.EUR
            };

            CurrentSession = await _salesService.CreateSessionAsync(createDto);

            if (CurrentSession == null)
            {
                _logger.LogWarning("CreateSessionAsync returned null");
                throw new InvalidOperationException("Failed to create session");
            }

            IsSessionClosed = false;

            _logger.LogInformation("Sale session created successfully: SessionId={SessionId}", CurrentSession.Id);
            NotifySuccess("Session created successfully");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(httpEx, "Unauthorized when creating sale session");
            NotifyError("Unauthorized access. Please log in again.");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(httpEx, "Forbidden when creating sale session");
            NotifyError("Access denied. Check tenant permissions.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            NotifyError($"Error creating session: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    private async Task CheckForSuspendedSessionsAsync()
    {
        try
        {
            _logger.LogDebug("Checking for suspended sessions...");
            var allSessions = await _salesService.GetActiveSessionsAsync();

            if (allSessions == null)
            {
                _logger.LogWarning("GetActiveSessionsAsync returned null");
                return;
            }

            var suspendedSessions = allSessions
                .Where(s => s.Status == SaleSessionStatusDto.Suspended)
                .ToList();

            _logger.LogDebug("Found {Count} suspended sessions", suspendedSessions.Count);

            // Note: The actual dialog handling is done in the UI layer
            // This method just checks if there are suspended sessions
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(httpEx, "Unauthorized error checking for suspended sessions");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(httpEx, "Forbidden error checking for suspended sessions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for suspended sessions");
        }
    }

    private async Task PrepareNewSaleAsync()
    {
        CurrentSession = null;
        SelectedCustomer = null;
        IsSessionClosed = false;
        NotifyStateChanged();

        if (_selectedPosId.HasValue)
        {
            await CreateNewSessionAsync();
        }
    }

    private async Task UpdateItemInternalAsync(SaleItemDto item)
    {
        if (CurrentSession == null) return;

        // Flush pending updates and acquire semaphore
        await FlushPendingUpdatesAsync();

        if (!await _updateSemaphore.WaitAsync(FlushTimeoutMs))
        {
            _logger.LogWarning("Timeout waiting for update lock in UpdateItemInternalAsync");
            return;
        }

        try
        {
            IsUpdatingItems = true;
            NotifyStateChanged();

            var updateDto = new UpdateSaleItemDto
            {
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountPercent = item.DiscountPercent,
                Notes = item.Notes
            };

            var updatedSession = await _salesService.UpdateItemAsync(CurrentSession.Id, item.Id, updateDto);

            if (updatedSession != null)
            {
                CurrentSession = updatedSession;
                NotifyStateChanged();
            }
            else
            {
                _logger.LogWarning("UpdateItemAsync returned null, reloading");
                await ReloadSessionAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item");
            await ReloadSessionAsync();
            NotifyError("Error updating item");
        }
        finally
        {
            _updateSemaphore.Release();
            IsUpdatingItems = false;
            NotifyStateChanged();
        }
    }

    private async Task UpdateItemWithRetryAsync(SaleItemDto item, int maxRetries = MaxRetryAttempts, CancellationToken cancellationToken = default)
    {
        if (CurrentSession == null)
            return;

        Exception? lastException = null;
        int totalAttempts = maxRetries + 1;

        for (int attempt = 0; attempt < totalAttempts; attempt++)
        {
            // Check for cancellation before each attempt
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("Update cancelled for item {ItemId}", item.Id);
                RollbackItem(item);
                return;
            }

            try
            {
                // Update retry state for UI
                _currentRetryAttempt = attempt;
                NotifyStateChanged();

                var updateDto = new UpdateSaleItemDto
                {
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    Notes = item.Notes
                };

                var updatedSession = await _salesService.UpdateItemAsync(CurrentSession.Id, item.Id, updateDto);

                if (updatedSession != null)
                {
                    CurrentSession = updatedSession;
                    // Success - reset retry counter and remove backup
                    _currentRetryAttempt = 0;
                    _itemBackups.Remove(item.Id);
                    NotifyStateChanged();
                    return;
                }
                else
                {
                    // Treat null response as a retryable error
                    _logger.LogWarning("UpdateItemAsync returned null in retry attempt {Attempt}/{Total}",
                        attempt + 1, totalAttempts);
                    if (attempt < maxRetries)
                    {
                        await HandleRetryableExceptionAsync(
                            new InvalidOperationException("API returned null response"),
                            "Null response", attempt, totalAttempts, cancellationToken);
                        continue;
                    }
                }
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                lastException = ex;
                await HandleRetryableExceptionAsync(ex, "Network error", attempt, totalAttempts, cancellationToken);
            }
            catch (TaskCanceledException ex) when (attempt < maxRetries)
            {
                lastException = ex;
                await HandleRetryableExceptionAsync(ex, "Timeout", attempt, totalAttempts, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Update was cancelled - rollback and rethrow
                _currentRetryAttempt = 0;
                _logger.LogDebug("Update cancelled for item {ItemId}", item.Id);
                RollbackItem(item);
                NotifyStateChanged();
                throw;
            }
            catch (Exception ex)
            {
                // Non-retryable error - rollback and fail immediately
                _currentRetryAttempt = 0;
                _logger.LogError(ex, "Non-retryable error updating item");
                RollbackItem(item);
                NotifyError("Error updating item");
                NotifyStateChanged();
                return;
            }
        }

        // All retries failed - rollback
        _currentRetryAttempt = 0;
        RollbackItem(item);
        NotifyStateChanged();
        NotifyError($"Failed to update item after {totalAttempts} attempts");
    }

    private async Task HandleRetryableExceptionAsync(Exception ex, string errorType, int attempt, int totalAttempts, CancellationToken cancellationToken)
    {
        var delayMs = CalculateBackoffDelayMs(attempt);
        _logger.LogWarning(ex, "{ErrorType} updating item (attempt {Attempt}/{Total}). Retrying in {Delay}ms...",
            errorType, attempt + 1, totalAttempts, delayMs);

        try
        {
            await Task.Delay(delayMs, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Retry delay cancelled");
            throw;
        }
    }

    private int CalculateBackoffDelayMs(int attempt)
    {
        return (int)Math.Pow(2, attempt) * 1000;
    }

    /// <summary>
    /// Rollback item to its original backed-up state on error.
    /// </summary>
    private void RollbackItem(SaleItemDto item)
    {
        if (_itemBackups.TryGetValue(item.Id, out var backup))
        {
            // Restore original values (all modifiable and calculated fields)
            item.Quantity = backup.Quantity;
            item.UnitPrice = backup.UnitPrice;
            item.DiscountPercent = backup.DiscountPercent;
            item.TotalAmount = backup.TotalAmount;
            item.TaxAmount = backup.TaxAmount;
            item.Notes = backup.Notes;

            _itemBackups.Remove(item.Id);
            _logger.LogInformation("Rolled back item {ItemId} to original state", item.Id);
        }
    }

    private async Task FlushPendingUpdatesAsync()
    {
        // Check if there are pending updates
        if (_pendingItemUpdates.Count == 0 || CurrentSession == null)
            return;

        // Try to acquire semaphore with timeout to prevent deadlock
        if (!await _updateSemaphore.WaitAsync(FlushTimeoutMs))
        {
            _logger.LogWarning("Timeout waiting for update lock in FlushPendingUpdatesAsync");
            throw new TimeoutException("Timeout waiting for pending updates to complete");
        }

        try
        {
            // Cancel any in-progress timer batch and stop timer
            _currentUpdateCts?.Cancel();
            _currentUpdateCts?.Dispose();
            _currentUpdateCts = null;
            _updateDebounceTimer?.Stop();

            // Get list of items to update
            var itemsToUpdate = _pendingItemUpdates.ToList();
            _pendingItemUpdates.Clear();

            if (itemsToUpdate.Count == 0)
            {
                return;
            }

            // Build dictionary for O(1) lookup
            var itemsDict = CurrentSession.Items
                .GroupBy(i => i.Id)
                .ToDictionary(g => g.Key, g => g.First());

            // Update each pending item without cancellation support (must complete)
            foreach (var itemId in itemsToUpdate)
            {
                if (itemsDict.TryGetValue(itemId, out var itemToUpdate))
                {
                    await UpdateItemWithRetryAsync(itemToUpdate, MaxRetryAttempts, CancellationToken.None);
                }
            }

            _logger.LogInformation("Flushed {Count} pending item updates before session action", itemsToUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing pending item updates");
            NotifyError("Error updating items");
            throw; // Re-throw to prevent session completion if updates fail
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        // Cancel any in-flight operations
        _currentUpdateCts?.Cancel();
        _currentUpdateCts?.Dispose();
        _currentUpdateCts = null;

        // Stop and dispose timer to prevent memory leaks
        if (_updateDebounceTimer != null)
        {
            _updateDebounceTimer.Stop();
            _updateDebounceTimer.Dispose();
            _updateDebounceTimer = null;
        }

        // Dispose semaphore
        _updateSemaphore?.Dispose();

        // Clear collections
        _pendingItemUpdates.Clear();
        _itemBackups.Clear();

        // Reset retry state
        _currentRetryAttempt = 0;
        IsUpdatingItems = false;
    }

    #endregion
}
