using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    
    /// <summary>
    /// DTO for StorageLocation output/display operations.
    /// </summary>
    public class StorageLocationDto
    {
        /// <summary>
        /// Unique identifier for the storage location.
        /// </summary>
        public Guid Id { get; set; }
    
        /// <summary>
        /// Location code (unique within the warehouse).
        /// </summary>
        public string Code { get; set; } = string.Empty;
    
        /// <summary>
        /// Description of the location.
        /// </summary>
        public string? Description { get; set; }
    
        /// <summary>
        /// Reference to the parent warehouse.
        /// </summary>
        public Guid WarehouseId { get; set; }
    
        /// <summary>
        /// Name of the parent warehouse.
        /// </summary>
        public string? WarehouseName { get; set; }
    
        /// <summary>
        /// Maximum capacity of the location.
        /// </summary>
        public int? Capacity { get; set; }
    
        /// <summary>
        /// Current occupancy of the location.
        /// </summary>
        public int? Occupancy { get; set; }
    
        /// <summary>
        /// Date of the last inventory check.
        /// </summary>
        public DateTime? LastInventoryDate { get; set; }
    
        /// <summary>
        /// Indicates if the location is refrigerated.
        /// </summary>
        public bool IsRefrigerated { get; set; }
    
        /// <summary>
        /// Additional notes or instructions for the location.
        /// </summary>
        public string? Notes { get; set; }
    
        /// <summary>
        /// Zone or area within the warehouse.
        /// </summary>
        public string? Zone { get; set; }
    
        /// <summary>
        /// Floor or level of the location.
        /// </summary>
        public string? Floor { get; set; }
    
        /// <summary>
        /// Row identifier.
        /// </summary>
        public string? Row { get; set; }
    
        /// <summary>
        /// Column identifier.
        /// </summary>
        public string? Column { get; set; }
    
        /// <summary>
        /// Level identifier.
        /// </summary>
        public string? Level { get; set; }
    
        /// <summary>
        /// Indicates whether the location is active.
        /// </summary>
        public bool IsActive { get; set; }
    
        /// <summary>
        /// Date and time when the storage location was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; set; }
    
        /// <summary>
        /// User who created the storage location.
        /// </summary>
        public string? CreatedBy { get; set; }
    
        /// <summary>
        /// Date and time when the storage location was last modified (UTC).
        /// </summary>
        public DateTime? ModifiedAt { get; set; }
    
        /// <summary>
        /// User who last modified the storage location.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
