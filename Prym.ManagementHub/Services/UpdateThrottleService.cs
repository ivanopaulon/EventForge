namespace Prym.ManagementHub.Services;

/// <summary>
/// Throttles concurrent update operations to respect <see cref="ManagementHubOptions.MaxConcurrentUpdates"/>.
/// When <c>MaxConcurrentUpdates</c> is 0 the throttle is bypassed (unlimited concurrency).
/// </summary>
public interface IUpdateThrottleService
{
    /// <summary>
    /// Acquires a throttle slot, waiting if the maximum number of concurrent updates is already running.
    /// Does nothing when <see cref="ManagementHubOptions.MaxConcurrentUpdates"/> is 0 (unlimited).
    /// </summary>
    Task AcquireAsync(CancellationToken ct = default);

    /// <summary>
    /// Releases a previously acquired throttle slot.
    /// Must be called exactly once after every successful <see cref="AcquireAsync"/>.
    /// Does nothing when throttling is disabled.
    /// </summary>
    void Release();

    /// <summary><see langword="true"/> when throttling is active (MaxConcurrentUpdates &gt; 0).</summary>
    bool IsThrottled { get; }
}

/// <inheritdoc />
public sealed class UpdateThrottleService : IUpdateThrottleService, IDisposable
{
    private readonly SemaphoreSlim? _semaphore;

    public UpdateThrottleService(ManagementHubOptions hubOptions)
    {
        if (hubOptions.MaxConcurrentUpdates > 0)
            _semaphore = new SemaphoreSlim(hubOptions.MaxConcurrentUpdates, hubOptions.MaxConcurrentUpdates);
    }

    public bool IsThrottled => _semaphore is not null;

    public Task AcquireAsync(CancellationToken ct = default)
        => _semaphore?.WaitAsync(ct) ?? Task.CompletedTask;

    public void Release()
        => _semaphore?.Release();

    public void Dispose()
        => _semaphore?.Dispose();
}
