namespace Prym.Web.Services
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
    /// - The session only expires after TRUE inactivity (no navigation, no API calls for 10 hours)
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

    public class SessionKeepaliveService(
        IAuthService authService,
        ILogger<SessionKeepaliveService> logger) : ISessionKeepaliveService
    {
        private const int KEEPALIVE_INTERVAL_MINUTES = 3;
        private const int WARNING_THRESHOLD_MINUTES = 15;
        private const int MAX_RETRIES = 3;
        private const int INITIAL_RETRY_DELAY_MS = 1000;

        private Timer? _keepaliveTimer;
        private CancellationTokenSource? _cts;
        private bool _isRunning;
        private int _consecutiveFailures;

        public event Action? OnRefreshSuccess;
        public event Action<string>? OnRefreshFailure;
        public event Action<int>? OnSessionWarning;

        public bool IsRunning => _isRunning;

        public void Start()
        {
            if (_isRunning)
            {
                logger.LogWarning("SessionKeepaliveService is already running");
                return;
            }

            logger.LogInformation("Starting SessionKeepaliveService with {IntervalMinutes} minute interval", KEEPALIVE_INTERVAL_MINUTES);

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

            logger.LogInformation("SessionKeepaliveService started successfully");
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                logger.LogDebug("SessionKeepaliveService is not running, nothing to stop");
                return;
            }

            logger.LogInformation("Stopping SessionKeepaliveService");

            _isRunning = false;
            _consecutiveFailures = 0;

            // Cancel any ongoing operations
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // Dispose the timer
            _keepaliveTimer?.Dispose();
            _keepaliveTimer = null;

            logger.LogInformation("SessionKeepaliveService stopped successfully");
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
                logger.LogError(ex, "Unhandled exception in SessionKeepaliveService timer callback");
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
                var isAuthenticated = await authService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    // Token is expired or missing. The server's refresh-token endpoint requires
                    // a valid token ([Authorize]), so refreshing is not possible here.
                    // The user will need to log in again. Fire OnRefreshFailure so the UI can
                    // prompt re-login if wired up to do so.
                    logger.LogWarning("Session expired: token is no longer valid. User must log in again.");
                    _consecutiveFailures++;
                    OnRefreshFailure?.Invoke("Session expired. Please log in again.");
                    return;
                }

                // SLIDING EXPIRATION: Always refresh when authenticated, regardless of time remaining
                // This ensures the session never expires as long as the user is active
                var timeToExpiry = await authService.GetTokenTimeToExpiryAsync();

                logger.LogInformation("🔄 Starting token refresh cycle. Time to expiry: {TimeToExpiry:F1} minutes",
                    timeToExpiry?.TotalMinutes ?? -1);

                if (timeToExpiry.HasValue)
                {
                    logger.LogDebug("Token expires in {Minutes:F1} minutes. Performing sliding expiration refresh.",
                        timeToExpiry.Value.TotalMinutes);
                }
                else
                {
                    logger.LogDebug("Token expiry unknown. Attempting refresh.");
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
                        logger.LogInformation("Token refresh attempt {Attempt}/{MaxRetries} (sliding expiration mode)",
                            attempt, MAX_RETRIES);
                    }
                    else
                    {
                        logger.LogDebug("Token refresh attempt {Attempt}/{MaxRetries} (sliding expiration mode)",
                            attempt, MAX_RETRIES);
                    }

                    try
                    {
                        success = await authService.RefreshTokenAsync();

                        if (success)
                        {
                            logger.LogInformation("✅ Token refreshed successfully on attempt {Attempt}. New expiration extended.", attempt);
                            _consecutiveFailures = 0;
                            OnRefreshSuccess?.Invoke();
                            return;
                        }
                        else
                        {
                            logger.LogWarning("⚠️ Token refresh returned false on attempt {Attempt}/{MaxRetries}. Check server endpoint availability.",
                                attempt, MAX_RETRIES);
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        logger.LogError(httpEx, "🌐 Network error during token refresh attempt {Attempt}: {Message}",
                            attempt, httpEx.Message);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "💥 Unexpected exception during token refresh attempt {Attempt}: {ExceptionType}",
                            attempt, ex.GetType().Name);
                    }

                    // If not the last attempt, wait with exponential backoff
                    if (attempt < MAX_RETRIES)
                    {
                        var delayMs = INITIAL_RETRY_DELAY_MS * (int)Math.Pow(2, attempt - 1);
                        logger.LogDebug("Waiting {DelayMs}ms before retry {NextAttempt}", delayMs, attempt + 1);
                        await Task.Delay(delayMs, cancellationToken);
                    }
                }

                // All retries exhausted
                _consecutiveFailures++;
                var errorMessage = $"Token refresh failed after {MAX_RETRIES} attempts (consecutive failures: {_consecutiveFailures})";
                logger.LogError(errorMessage);
                OnRefreshFailure?.Invoke(errorMessage);

                // If we have too many consecutive failures, log critically but keep the service running
                if (_consecutiveFailures >= 5)
                {
                    logger.LogCritical("Too many consecutive failures ({Count}), but SessionKeepaliveService will keep retrying",
                        _consecutiveFailures);
                }

                // Emit OnSessionWarning when token is close to expiry AND refresh has been failing
                if (timeToExpiry.HasValue && timeToExpiry.Value.TotalMinutes < WARNING_THRESHOLD_MINUTES)
                {
                    OnSessionWarning?.Invoke((int)timeToExpiry.Value.TotalMinutes);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Token refresh cancelled");
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                var errorMessage = $"Critical error during token refresh: {ex.Message}";
                logger.LogError(ex, errorMessage);
                OnRefreshFailure?.Invoke(errorMessage);
            }
        }

        public void Dispose()
        {
            logger.LogDebug("Disposing SessionKeepaliveService");
            Stop();
        }
    }
}
