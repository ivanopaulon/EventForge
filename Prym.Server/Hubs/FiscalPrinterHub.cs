using Prym.DTOs.FiscalPrinting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Prym.Server.Hubs;

/// <summary>
/// SignalR hub for real-time fiscal printer status notifications.
/// Clients subscribe to individual printer groups and receive push updates
/// when the background monitoring service polls a printer.
/// </summary>
/// <remarks>
/// Group naming convention: <c>Printer_{printerId:N}</c> (Guid without dashes).
/// Server-to-client methods:
/// <list type="bullet">
///   <item><see cref="PrinterStatusUpdated"/> – emitted after every polling cycle.</item>
///   <item><see cref="ClosureRequired"/> – emitted when the daily closure flag is set.</item>
///   <item><see cref="CriticalClosureMissing"/> – emitted when fiscal memory is full.</item>
/// </list>
/// </remarks>
[Authorize]
public class FiscalPrinterHub(ILogger<FiscalPrinterHub> logger) : Hub
{
    // -------------------------------------------------------------------------
    //  Connection lifecycle
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        logger.LogDebug(
            "FiscalPrinterHub: user {UserId} connected (ConnectionId={ConnectionId})",
            userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (exception is not null)
        {
            logger.LogWarning(exception,
                "FiscalPrinterHub: user {UserId} disconnected with error (ConnectionId={ConnectionId})",
                userId, Context.ConnectionId);
        }
        else
        {
            logger.LogDebug(
                "FiscalPrinterHub: user {UserId} disconnected gracefully (ConnectionId={ConnectionId})",
                userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // -------------------------------------------------------------------------
    //  Client-callable methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Subscribes the calling client to push status updates for the specified printer.
    /// After subscribing the client will receive <c>PrinterStatusUpdated</c> messages
    /// whenever the monitor service polls that printer.
    /// </summary>
    /// <param name="printerId">Unique identifier of the fiscal printer to watch.</param>
    public async Task SubscribeToPrinter(Guid printerId)
    {
        var groupName = PrinterGroupName(printerId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        logger.LogInformation(
            "FiscalPrinterHub: connection {ConnectionId} subscribed to printer {PrinterId}",
            Context.ConnectionId, printerId);
    }

    /// <summary>
    /// Unsubscribes the calling client from push status updates for the specified printer.
    /// </summary>
    /// <param name="printerId">Unique identifier of the fiscal printer to stop watching.</param>
    public async Task UnsubscribeFromPrinter(Guid printerId)
    {
        var groupName = PrinterGroupName(printerId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        logger.LogInformation(
            "FiscalPrinterHub: connection {ConnectionId} unsubscribed from printer {PrinterId}",
            Context.ConnectionId, printerId);
    }

    // -------------------------------------------------------------------------
    //  Server-to-client method names (constants for use in IHubContext calls)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Name of the server-to-client method emitted after every polling cycle.
    /// Signature: <c>PrinterStatusUpdated(Guid printerId, FiscalPrinterStatus status)</c>.
    /// </summary>
    public const string PrinterStatusUpdated = "PrinterStatusUpdated";

    /// <summary>
    /// Name of the server-to-client method emitted when the daily closure flag is set.
    /// Signature: <c>ClosureRequired(Guid printerId, string printerName)</c>.
    /// </summary>
    public const string ClosureRequired = "ClosureRequired";

    /// <summary>
    /// Name of the server-to-client method emitted when fiscal memory is full.
    /// Signature: <c>CriticalClosureMissing(Guid printerId)</c>.
    /// </summary>
    public const string CriticalClosureMissing = "CriticalClosureMissing";

    // -------------------------------------------------------------------------
    //  Helpers
    // -------------------------------------------------------------------------

    /// <summary>Returns the SignalR group name for a given printer ID.</summary>
    public static string PrinterGroupName(Guid printerId) => $"Printer_{printerId:N}";

    private string? GetCurrentUserId()
        => Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}
