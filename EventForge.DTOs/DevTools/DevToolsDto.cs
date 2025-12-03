using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.DevTools
{
    /// <summary>
    /// Request DTO per avviare la generazione di prodotti di test.
    /// </summary>
    public class GenerateProductsRequestDto
    {
        /// <summary>
        /// Numero di prodotti da generare (default: 5000).
        /// </summary>
        [Range(1, 20000, ErrorMessage = "Il numero di prodotti deve essere compreso tra 1 e 20000.")]
        public int Count { get; set; } = 5000;

        /// <summary>
        /// Dimensione del batch per le insert (default: 100).
        /// </summary>
        [Range(10, 1000, ErrorMessage = "La dimensione del batch deve essere compresa tra 10 e 1000.")]
        public int BatchSize { get; set; } = 100;
    }

    /// <summary>
    /// DTO per lo stato di un job di generazione prodotti.
    /// </summary>
    public class GenerateProductsStatusDto
    {
        /// <summary>
        /// ID univoco del job.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Stato corrente del job.
        /// </summary>
        public ProductGenerationJobStatus Status { get; set; }

        /// <summary>
        /// Numero di prodotti elaborati finora.
        /// </summary>
        public int Processed { get; set; }

        /// <summary>
        /// Numero totale di prodotti da creare.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Numero di prodotti creati con successo.
        /// </summary>
        public int Created { get; set; }

        /// <summary>
        /// Numero di errori riscontrati.
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Messaggio di errore se il job è fallito.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp di inizio del job.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Timestamp di completamento del job.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Durata del job in secondi.
        /// </summary>
        public double? DurationSeconds { get; set; }
    }

    /// <summary>
    /// Stato di un job di generazione prodotti.
    /// </summary>
    public enum ProductGenerationJobStatus
    {
        /// <summary>
        /// Job in attesa di avvio.
        /// </summary>
        Pending,

        /// <summary>
        /// Job in esecuzione.
        /// </summary>
        Running,

        /// <summary>
        /// Job completato con successo.
        /// </summary>
        Done,

        /// <summary>
        /// Job fallito.
        /// </summary>
        Failed,

        /// <summary>
        /// Job cancellato dall'utente.
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// DTO di risposta per l'avvio di un job di generazione prodotti.
    /// </summary>
    public class GenerateProductsResponseDto
    {
        /// <summary>
        /// ID univoco del job avviato.
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Messaggio informativo.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp di avvio del job.
        /// </summary>
        public DateTime StartedAt { get; set; }
    }
}
