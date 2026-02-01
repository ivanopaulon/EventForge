namespace EventForge.Client.Services
{
    /// <summary>
    /// Global service for maintaining user sessions across all pages by automatically refreshing JWT tokens.
    /// This service runs a background timer that periodically checks and refreshes the authentication token
    /// to prevent session expiration while the user is active.
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
        private const int REFRESH_THRESHOLD_MINUTES = 30; // Rinnovare quando mancano 30 minuti invece di 10
        private const int WARNING_THRESHOLD_MINUTES = 15; // Mostrare warning solo sotto i 15 minuti
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
        public event Action<int>? OnSessionWarning; // Parametro: minuti rimanenti

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

                // Check time to expiry
                var timeToExpiry = await _authService.GetTokenTimeToExpiryAsync();
                if (timeToExpiry.HasValue)
                {
                    var minutesRemaining = timeToExpiry.Value.TotalMinutes;
                    _logger.LogInformation("Token expires in {Minutes:F1} minutes", minutesRemaining);

                    // If token has plenty of time left (more than refresh threshold), skip refresh
                    if (minutesRemaining > REFRESH_THRESHOLD_MINUTES)
                    {
                        _logger.LogDebug("Token still has {Minutes:F1} minutes, skipping refresh (threshold: {Threshold} min)", 
                            minutesRemaining, REFRESH_THRESHOLD_MINUTES);
                        _consecutiveFailures = 0; // Reset failure counter on successful check
                        return;
                    }

                    // If token is getting low but not critical, attempt silent refresh
                    if (minutesRemaining > WARNING_THRESHOLD_MINUTES)
                    {
                        _logger.LogInformation("Token will expire in {Minutes:F1} minutes, attempting proactive refresh", minutesRemaining);
                    }
                    else
                    {
                        // Token is in critical range, notify UI
                        _logger.LogWarning("Token will expire in {Minutes:F1} minutes (critical threshold), attempting urgent refresh", minutesRemaining);
                        OnSessionWarning?.Invoke((int)Math.Ceiling(minutesRemaining));
                    }
                }

                // Attempt refresh with retry logic
                // Note: AuthService.RefreshTokenAsync has its own internal retry (max 2 attempts) for 5xx errors.
                // This outer retry handles overall operation failures including network issues, 
                // ensuring robust session management at the application level.
                bool success = false;
                for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    _logger.LogInformation("Token refresh attempt {Attempt}/{MaxRetries}", attempt, MAX_RETRIES);

                    try
                    {
                        success = await _authService.RefreshTokenAsync();

                        if (success)
                        {
                            _logger.LogInformation("Token refresh succeeded on attempt {Attempt}", attempt);
                            _consecutiveFailures = 0;
                            OnRefreshSuccess?.Invoke();
                            return;
                        }
                        else
                        {
                            _logger.LogWarning("Token refresh failed on attempt {Attempt}", attempt);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception during token refresh attempt {Attempt}", attempt);
                    }

                    // If not the last attempt, wait with exponential backoff
                    if (attempt < MAX_RETRIES)
                    {
                        var delayMs = INITIAL_RETRY_DELAY_MS * (int)Math.Pow(2, attempt - 1); // Exponential: 1s, 2s, 4s
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
                    _logger.LogCritical("Too many consecutive failures ({Count}), stopping SessionKeepaliveService", _consecutiveFailures);
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
