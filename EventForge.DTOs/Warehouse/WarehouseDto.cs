namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// Simplified DTO for Warehouse selection in dialogs and forms.
    /// This is an alias/simplified version of StorageFacilityDto with only essential fields.
    /// </summary>
    public class WarehouseDto
    {
        /// <summary>
        /// Unique identifier for the warehouse.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the warehouse.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unique code for the warehouse.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Indicates if the warehouse is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
