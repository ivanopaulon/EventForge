namespace EventForge.Client.Services
{
    /// <summary>
    /// Global service for maintaining user sessions across all pages by automatically refreshing JWT tokens.
    /// This service runs a background timer that periodically checks and refreshes the authentication token
    /// to prevent session expiration while the user is active.
    /// 
    /// SLIDING EXPIRATION STRATEGY:
    /// This service implements a true sliding expiration pattern where the JWT token is refreshed
    /// every KEEPALIVE_INTERVAL_MINUTES (3 minutes) as long as the user is authenticated.
    /// 
    /// This ensures that:
    /// - Users NEVER have to re-login during active work sessions
    /// - The session only expires after TRUE inactivity (no navigation, no API calls for 4 hours)
    /// - Token expiration acts as a safety buffer, not an active session timeout
    /// 
    /// The token is also refreshed on:
    /// - Every page navigation (see MainLayout.OnLocationChanged)
    /// - Every API call that uses the authenticated HttpClient
    /// </summary>
    public interface ISessionKeepaliveService : IDisposable
    {
        /// <summary>
        /// Event raised when token refresh succeeds
        /// </summary>
        event Action? OnRefreshSuccess;

        /// <summary>
        /// Event raised when token refresh fails
        /// </summary>
        event Action<string>? OnRefreshFailure;

        /// <summary>
        /// Event raised when session is in warning state (low time remaining)
        /// </summary>
        event Action<int>? OnSessionWarning;

        /// <summary>
        /// Starts the keepalive timer
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the keepalive timer
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets whether the keepalive service is currently running
        /// </summary>
        bool IsRunning { get; }
    }

    public class SessionKeepaliveService : ISessionKeepaliveService
    {
        private const int KEEPALIVE_INTERVAL_MINUTES = 3;
        private const int WARNING_THRESHOLD_MINUTES = 15; // Trigger urgent refresh below 15 minutes (UI shows warning only at < 10 min)
        private const int MAX_RETRIES = 3;
        private const int INITIAL_RETRY_DELAY_MS = 1000; // 1 second

        private readonly IAuthService _authService;
        private readonly ILogger<SessionKeepaliveService> _logger;
        private Timer? _keepaliveTimer;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private int _consecutiveFailures;

        public event Action? OnRefreshSuccess;
        public event Action<string>? OnRefreshFailure;
        public event Action<int>? OnSessionWarning; // Parameter: minutes remaining

        public bool IsRunning => _isRunning;

        public SessionKeepaliveService(IAuthService authService, ILogger<SessionKeepaliveService> logger)
        {
            _authService = authService;
            _logger = logger;
            _consecutiveFailures = 0;
        }

        public void Start()
        {
            if (_isRunning)
            {
                _logger.LogWarning("SessionKeepaliveService is already running");
                return;
            }

            _logger.LogInformation("Starting SessionKeepaliveService with {IntervalMinutes} minute interval", KEEPALIVE_INTERVAL_MINUTES);

            _cts = new CancellationTokenSource();
            _isRunning = true;
            _consecutiveFailures = 0;

            // Start the timer - first tick after the interval, then repeat every interval
            _keepaliveTimer = new Timer(
                OnTimerCallback,
                null,
                TimeSpan.FromMinutes(KEEPALIVE_INTERVAL_MINUTES),
                TimeSpan.FromMinutes(KEEPALIVE_INTERVAL_MINUTES)
            );

            _logger.LogInformation("SessionKeepaliveService started successfully");
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                _logger.LogDebug("SessionKeepaliveService is not running, nothing to stop");
                return;
            }

            _logger.LogInformation("Stopping SessionKeepaliveService");

            _isRunning = false;
            _consecutiveFailures = 0;

            // Cancel any ongoing operations
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // Dispose the timer
            _keepaliveTimer?.Dispose();
            _keepaliveTimer = null;

            _logger.LogInformation("SessionKeepaliveService stopped successfully");
        }

        private void OnTimerCallback(object? state)
        {
            // Prevent re-entrance if previous tick is still running
            if (_cts?.IsCancellationRequested == true)
            {
                return;
            }

            try
            {
                // Execute the refresh asynchronously but don't await to avoid blocking the timer thread
                _ = Task.Run(async () => await RefreshTokenWithRetryAsync(_cts?.Token ?? default));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in SessionKeepaliveService timer callback");
            }
        }

        private async Task RefreshTokenWithRetryAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                // Check if user is authenticated before attempting refresh
                var isAuthenticated = await _authService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    _logger.LogDebug("User not authenticated, skipping token refresh");
                    return;
                }

                // SLIDING EXPIRATION: Always refresh when authenticated, regardless of time remaining
                // This ensures the session never expires as long as the user is active
                var timeToExpiry = await _authService.GetTokenTimeToExpiryAsync();
                if (timeToExpiry.HasValue)
                {
                    _logger.LogDebug("Token expires in {Minutes:F1} minutes. Performing sliding expiration refresh.", 
                        timeToExpiry.Value.TotalMinutes);
                }
                else
                {
                    _logger.LogDebug("Token expiry unknown. Attempting refresh.");
                }

                // Attempt refresh with retry logic
                bool success = false;
                for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (attempt > 1)
                    {
                        _logger.LogInformation("Token refresh attempt {Attempt}/{MaxRetries} (sliding expiration mode)", 
                            attempt, MAX_RETRIES);
                    }
                    else
                    {
                        _logger.LogDebug("Token refresh attempt {Attempt}/{MaxRetries} (sliding expiration mode)", 
                            attempt, MAX_RETRIES);
                    }

                    try
                    {
                        success = await _authService.RefreshTokenAsync();

                        if (success)
                        {
                            _logger.LogInformation("Token refreshed successfully on attempt {Attempt}. Session extended.", attempt);
                            _consecutiveFailures = 0;
                            OnRefreshSuccess?.Invoke();
                            return;
                        }
                        else
                        {
                            _logger.LogWarning("Token refresh returned false on attempt {Attempt}", attempt);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception during token refresh attempt {Attempt}", attempt);
                    }

                    // If not the last attempt, wait with exponential backoff
                    if (attempt < MAX_RETRIES)
                    {
                        var delayMs = INITIAL_RETRY_DELAY_MS * (int)Math.Pow(2, attempt - 1);
                        _logger.LogDebug("Waiting {DelayMs}ms before retry {NextAttempt}", delayMs, attempt + 1);
                        await Task.Delay(delayMs, cancellationToken);
                    }
                }

                // All retries exhausted
                _consecutiveFailures++;
                var errorMessage = $"Token refresh failed after {MAX_RETRIES} attempts (consecutive failures: {_consecutiveFailures})";
                _logger.LogError(errorMessage);
                OnRefreshFailure?.Invoke(errorMessage);

                // If we have too many consecutive failures, stop the service
                if (_consecutiveFailures >= 5)
                {
                    _logger.LogCritical("Too many consecutive failures ({Count}), stopping SessionKeepaliveService", 
                        _consecutiveFailures);
                    Stop();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Token refresh cancelled");
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                var errorMessage = $"Critical error during token refresh: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                OnRefreshFailure?.Invoke(errorMessage);
            }
        }

        public void Dispose()
        {
            _logger.LogDebug("Disposing SessionKeepaliveService");
            Stop();
        }
    }
}
