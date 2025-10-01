using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{
    /// <summary>
    /// DTO for project order information.
    /// </summary>
    public class ProjectOrderDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProjectName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        [Required]
        public string ProjectType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        [StringLength(100)]
        public string? ProjectManager { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedBudget { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ActualCost { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedHours { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ActualHours { get; set; }

        [Range(0, 100)]
        public decimal ProgressPercentage { get; set; }

        public Guid? StorageLocationId { get; set; }
        public string? StorageLocationName { get; set; }

        public Guid? DocumentId { get; set; }

        [StringLength(100)]
        public string? ExternalReference { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public int MaterialAllocationCount { get; set; }
        public int StockMovementCount { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for creating a project order.
    /// </summary>
    public class CreateProjectOrderDto
    {
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProjectName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public Guid? CustomerId { get; set; }

        [Required]
        public string ProjectType { get; set; } = string.Empty;

        public string? Status { get; set; }

        public string? Priority { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }

        [StringLength(100)]
        public string? ProjectManager { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedBudget { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedHours { get; set; }

        public Guid? StorageLocationId { get; set; }

        public Guid? DocumentId { get; set; }

        [StringLength(100)]
        public string? ExternalReference { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for updating a project order.
    /// </summary>
    public class UpdateProjectOrderDto
    {
        [StringLength(200)]
        public string? ProjectName { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public string? Status { get; set; }

        public string? Priority { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }

        [StringLength(100)]
        public string? ProjectManager { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedBudget { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ActualCost { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedHours { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ActualHours { get; set; }

        [Range(0, 100)]
        public decimal? ProgressPercentage { get; set; }

        public Guid? StorageLocationId { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for project material allocation information.
    /// </summary>
    public class ProjectMaterialAllocationDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public Guid ProjectOrderId { get; set; }
        public string? ProjectOrderNumber { get; set; }
        public string? ProjectName { get; set; }

        [Required]
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }

        public Guid? LotId { get; set; }
        public string? LotCode { get; set; }

        public Guid? SerialId { get; set; }
        public string? SerialNumber { get; set; }

        public Guid? StorageLocationId { get; set; }
        public string? StorageLocationName { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PlannedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AllocatedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ConsumedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ReturnedQuantity { get; set; }

        public Guid? UnitOfMeasureId { get; set; }
        public string? UnitOfMeasureName { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? PlannedDate { get; set; }
        public DateTime? AllocationDate { get; set; }
        public DateTime? ConsumptionStartDate { get; set; }
        public DateTime? ConsumptionEndDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitCost { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? TotalCost { get; set; }

        public Guid? StockMovementId { get; set; }

        [StringLength(200)]
        public string? Purpose { get; set; }

        [StringLength(100)]
        public string? RequestedBy { get; set; }

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// DTO for creating a project material allocation.
    /// </summary>
    public class CreateProjectMaterialAllocationDto
    {
        [Required]
        public Guid ProjectOrderId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        public Guid? LotId { get; set; }
        public Guid? SerialId { get; set; }
        public Guid? StorageLocationId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PlannedQuantity { get; set; }

        public Guid? UnitOfMeasureId { get; set; }

        public DateTime? PlannedDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitCost { get; set; }

        [StringLength(200)]
        public string? Purpose { get; set; }

        [StringLength(100)]
        public string? RequestedBy { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for updating a project material allocation.
    /// </summary>
    public class UpdateProjectMaterialAllocationDto
    {
        [Range(0, double.MaxValue)]
        public decimal? AllocatedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ConsumedQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ReturnedQuantity { get; set; }

        public string? Status { get; set; }

        public DateTime? AllocationDate { get; set; }
        public DateTime? ConsumptionStartDate { get; set; }
        public DateTime? ConsumptionEndDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? UnitCost { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? TotalCost { get; set; }

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
