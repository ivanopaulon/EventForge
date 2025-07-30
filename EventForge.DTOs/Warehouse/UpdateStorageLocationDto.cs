using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    
    /// <summary>
    /// DTO for updating an existing storage location.
    /// </summary>
    public class UpdateStorageLocationDto
    {
        /// <summary>
        /// Location code (unique within the warehouse).
        /// </summary>
        [StringLength(30, ErrorMessage = "Location code cannot exceed 30 characters.")]
        public string? Code { get; set; }
    
        /// <summary>
        /// Description of the location.
        /// </summary>
        [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
        public string? Description { get; set; }
    
        /// <summary>
        /// Reference to the parent warehouse.
        /// </summary>
        public Guid? WarehouseId { get; set; }
    
        /// <summary>
        /// Maximum capacity of the location.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Capacity must be non-negative.")]
        public int? Capacity { get; set; }
    
        /// <summary>
        /// Current occupancy of the location.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Occupancy must be non-negative.")]
        public int? Occupancy { get; set; }
    
        /// <summary>
        /// Indicates if the location is refrigerated.
        /// </summary>
        public bool? IsRefrigerated { get; set; }
    
        /// <summary>
        /// Additional notes or instructions for the location.
        /// </summary>
        [StringLength(200, ErrorMessage = "Notes cannot exceed 200 characters.")]
        public string? Notes { get; set; }
    
        /// <summary>
        /// Zone or area within the warehouse.
        /// </summary>
        [StringLength(20, ErrorMessage = "Zone cannot exceed 20 characters.")]
        public string? Zone { get; set; }
    
        /// <summary>
        /// Floor or level of the location.
        /// </summary>
        [StringLength(10, ErrorMessage = "Floor cannot exceed 10 characters.")]
        public string? Floor { get; set; }
    
        /// <summary>
        /// Row identifier.
        /// </summary>
        [StringLength(10, ErrorMessage = "Row cannot exceed 10 characters.")]
        public string? Row { get; set; }
    
        /// <summary>
        /// Column identifier.
        /// </summary>
        [StringLength(10, ErrorMessage = "Column cannot exceed 10 characters.")]
        public string? Column { get; set; }
    
        /// <summary>
        /// Level identifier.
        /// </summary>
        [StringLength(10, ErrorMessage = "Level cannot exceed 10 characters.")]
        public string? Level { get; set; }
    
        /// <summary>
        /// Indicates whether the location is active.
        /// </summary>
        public bool? IsActive { get; set; }
    }
}
