using Microsoft.AspNetCore.Mvc;
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

    /// <summary>
    /// Sends a sample receipt to the specified printer and redirects back with a toast message.
    /// </summary>
    public async Task<IActionResult> OnPostTestPrintAsync(string printerName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            TempData["Error"] = "Nome stampante non specificato.";
            return RedirectToPage();
        }

        logger.LogInformation("Devices page: test print requested for '{Printer}'", printerName);

        var ok = await printerService.SendTestPrintAsync(printerName, ct);

        if (ok)
            TempData["Success"] = $"Stampa di prova inviata a «{printerName}». Verificare che la pagina venga stampata correttamente.";
        else
            TempData["Error"] = $"Impossibile inviare la stampa di prova a «{printerName}». Verificare che la stampante sia online e il nome sia corretto.";

        return RedirectToPage();
    }
}
