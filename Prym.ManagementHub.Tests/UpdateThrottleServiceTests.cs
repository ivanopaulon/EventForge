using Prym.ManagementHub.Configuration;
using Prym.ManagementHub.Services;

namespace Prym.ManagementHub.Tests;

/// <summary>
/// Tests for <see cref="UpdateThrottleService"/>.
/// Verifies that the acquire/release balance is always maintained — no slot leaks
/// and no over-releases that would throw <see cref="SemaphoreFullException"/>.
/// </summary>
public class UpdateThrottleServiceTests
{
    // ── Basic acquire/release ───────────────────────────────────────────────

    [Fact]
    public async Task AcquireAndRelease_OnSlot_DoesNotLeak()
    {
        var svc = BuildThrottle(maxConcurrent: 1);

        await svc.AcquireAsync();
        svc.Release();

        // A second acquire should succeed immediately (no leak from the first cycle).
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await svc.AcquireAsync(cts.Token);
        svc.Release();
    }

    [Fact]
    public async Task Release_CalledExactlyAsManyTimesAsAcquire_NoSemaphoreFullException()
    {
        var svc = BuildThrottle(maxConcurrent: 2);

        await svc.AcquireAsync();
        await svc.AcquireAsync();

        // Release both — no exception expected.
        svc.Release();
        svc.Release();
    }

    [Fact]
    public void Release_WithoutPriorAcquire_ThrowsSemaphoreFullException()
    {
        // An over-release (more releases than acquires) must surface as SemaphoreFullException
        // so callers can detect programming errors. This test documents the contract.
        var svc = BuildThrottle(maxConcurrent: 1);

        Assert.Throws<SemaphoreFullException>(() => svc.Release());
    }

    [Fact]
    public async Task Acquire_WhenSlotsFull_BlocksUntilRelease()
    {
        var svc = BuildThrottle(maxConcurrent: 1);
        await svc.AcquireAsync();

        // A second acquire on a background task should be blocked.
        var acquireTask = Task.Run(async () => await svc.AcquireAsync());
        await Task.Delay(50); // give the task a moment to start
        Assert.False(acquireTask.IsCompleted, "Second acquire should be blocked while slot is held.");

        // Releasing the slot should unblock the waiting acquire.
        svc.Release();
        await acquireTask.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(acquireTask.IsCompletedSuccessfully);

        // Clean up.
        svc.Release();
    }

    [Fact]
    public async Task Unlimited_ThrottleBypassedCompletely()
    {
        var svc = BuildThrottle(maxConcurrent: 0); // 0 = unlimited

        Assert.False(svc.IsThrottled);

        // Acquire should complete immediately even without a semaphore.
        await svc.AcquireAsync();
        await svc.AcquireAsync();

        // Release should not throw even without a backing semaphore.
        svc.Release();
        svc.Release();
    }

    [Fact]
    public async Task Acquire_CancelledBeforeAcquire_ThrowsOperationCanceled_AndDoesNotHoldSlot()
    {
        var svc = BuildThrottle(maxConcurrent: 1);

        // Fill the single slot.
        await svc.AcquireAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Attempting to acquire with an already-cancelled token should throw without holding a slot.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => svc.AcquireAsync(cts.Token));

        // The slot should still be available after the cancelled acquire.
        svc.Release(); // release the original slot

        var acquireAfterCancel = svc.AcquireAsync();
        await acquireAfterCancel.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(acquireAfterCancel.IsCompletedSuccessfully, "Slot should be re-acquirable after a cancelled attempt.");
        svc.Release();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static UpdateThrottleService BuildThrottle(int maxConcurrent)
    {
        var options = new ManagementHubOptions { MaxConcurrentUpdates = maxConcurrent };
        return new UpdateThrottleService(options);
    }
}
