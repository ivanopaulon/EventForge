using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventForge.UpdateHub.Pages;

public class IndexModel(
    IInstallationService installationService,
    IPackageService packageService,
    IConnectionTracker connectionTracker) : PageModel
{
    public IReadOnlyList<Installation> Installations { get; private set; } = [];
    public int TotalInstallations { get; private set; }
    public int OnlineCount { get; private set; }
    public int OfflineCount { get; private set; }
    public int PackageCount { get; private set; }

    public async Task OnGetAsync()
    {
        Installations = await installationService.GetAllAsync();
        var onlineIds = connectionTracker.GetOnlineInstallationIds();
        TotalInstallations = Installations.Count;
        OnlineCount = onlineIds.Count;
        OfflineCount = TotalInstallations - OnlineCount;
        PackageCount = (await packageService.GetAllAsync()).Count;
    }
}
