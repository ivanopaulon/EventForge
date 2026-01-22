using EventForge.DTOs.Common;
using System;
using System.Collections.Generic;

namespace EventForge.DTOs.PriceLists
{

    /// <summary>
    /// DTO for PriceList output/display operations.
    /// </summary>
    public class PriceListDto
    {
        /// <summary>
        /// Unique identifier for the price list.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the price list.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unique code for the price list.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Description of the price list.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Start date of the price list validity.
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// End date of the price list validity.
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// Additional notes for the price list.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Status of the price list.
        /// </summary>
        public PriceListStatus Status { get; set; }

        /// <summary>
        /// Indicates if this is the default price list for the event.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Priority of the price list (0 = highest priority).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Tipo di listino (Sales/Purchase).
        /// </summary>
        public PriceListType Type { get; set; }

        /// <summary>
        /// Direzione del listino (Output/Input).
        /// </summary>
        public PriceListDirection Direction { get; set; }

        /// <summary>
        /// Associated event ID.
        /// </summary>
        public Guid? EventId { get; set; }

        /// <summary>
        /// Event name (for display purposes).
        /// </summary>
        public string? EventName { get; set; }

        /// <summary>
        /// Number of price list entries.
        /// </summary>
        public int EntryCount { get; set; }

        /// <summary>
        /// Date and time when the price list was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// User who created the price list.
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the price list was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// User who last modified the price list.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// BusinessParties assegnati a questo listino.
        /// </summary>
        public List<PriceListBusinessPartyDto> AssignedBusinessParties { get; set; } = new();
    }
}
