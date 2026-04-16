namespace Prym.Web.Services;

/// <summary>
/// Service implementation for managing authentication dialogs
/// </summary>
public class AuthenticationDialogService : IAuthenticationDialogService
{
    public event Func<TaskCompletionSource<bool>, Task>? LoginRequested;

    public async Task<bool> ShowLoginDialogAsync(CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (LoginRequested != null)
            await LoginRequested.Invoke(tcs);
        else
            tcs.SetResult(false); // fallback: no overlay registered

        return await tcs.Task;
    }
}
