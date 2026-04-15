using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Prym.Agent.Pages;

/// <summary>
/// Devices page: lists all locally-connected devices visible to the agent —
/// raw USB printer ports, OS-installed printer queues, and serial (COM) ports.
/// </summary>
public class DevicesModel(
    AgentOptions agentOptions,
    IAgentPrinterService printerService,
    ILogger<DevicesModel> logger) : PageModel
{
    /// <summary>Raw USB device identifiers accessible via <c>\\.\USB0xx</c> (e.g. <c>USB001</c>).</summary>
    public IReadOnlyList<string> UsbDevices { get; private set; } = [];

    /// <summary>Printer queues installed at OS level (Windows: Get-Printer; Linux: lpstat -a).</summary>
    public IReadOnlyList<string> SystemPrinters { get; private set; } = [];

    /// <summary>Serial / COM ports present on the machine (Windows: COMx; Linux: /dev/ttySx etc.).</summary>
    public IReadOnlyList<string> SerialPorts { get; private set; } = [];

    public string InstallationId   => agentOptions.InstallationId;
    public string InstallationName => agentOptions.InstallationName;

    public async Task OnGetAsync(CancellationToken ct)
    {
        try
        {
            var printersTask = printerService.ListSystemPrintersAsync(ct);
            var serialTask   = printerService.ListSerialPortsAsync(ct);

            // ListDevices() is synchronous; run on a thread-pool thread so it does not
            // block the request thread while probing up to 99 USB device paths in parallel.
            var usbTask = Task.Run(() => printerService.ListDevices(), ct);

            await Task.WhenAll(usbTask, printersTask, serialTask).ConfigureAwait(false);

            UsbDevices     = await usbTask;
            SystemPrinters = await printersTask;
            SerialPorts    = await serialTask;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Devices page: error enumerating devices");
        }
    }
}
