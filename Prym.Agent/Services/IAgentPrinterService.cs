namespace Prym.Agent.Services;

/// <summary>
/// Abstraction for communicating with a USB-attached printer on the local machine.
/// </summary>
public interface IAgentPrinterService
{
    /// <summary>
    /// Sends a raw command to a USB printer identified by <paramref name="deviceId"/>
    /// and returns the printer's response bytes.
    /// </summary>
    /// <param name="deviceId">
    /// The Windows device path suffix, e.g. <c>USB001</c> (resolved to <c>\\.\USB001</c>).
    /// </param>
    /// <param name="command">Raw command bytes to write to the device.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response bytes read from the device after writing the command.</returns>
    Task<byte[]> SendCommandAsync(string deviceId, byte[] command, CancellationToken ct = default);

    /// <summary>
    /// Verifies that the device at <paramref name="deviceId"/> can be opened for writing.
    /// Throws <see cref="InvalidOperationException"/> when the device is not accessible.
    /// </summary>
    /// <param name="deviceId">The Windows device path suffix, e.g. <c>USB001</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task TestConnectionAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Enumerates USB printer device paths that exist on the local machine
    /// (probes <c>\\.\USB001</c> through <c>\\.\USB009</c>).
    /// </summary>
    /// <returns>Read-only list of accessible device path suffixes (e.g. <c>USB001</c>).</returns>
    IReadOnlyList<string> ListDevices();
}
