namespace EventForge.Server.Data.Entities.StationMonitor;


/// <summary>
/// Status of a station order queue item.
/// </summary>
public enum StationOrderQueueStatus
{
    Waiting,        // In attesa
    Accepted,       // Preso in carico
    InPreparation,  // In preparazione
    Ready,          // Pronto per la consegna
    Delivered,      // Consegnato
    Cancelled       // Annullato
}