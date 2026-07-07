using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Sales
{

    /// <summary>
    /// DTO for table session information.
    /// </summary>
    public class TableSessionDto
    {
        public Guid Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string? TableName { get; set; }
        public int Capacity { get; set; }
        public TableShapeDto Shape { get; set; } = TableShapeDto.Rectangle;
        public int Width { get; set; } = 90;
        public int Height { get; set; } = 90;
        public string Status { get; set; } = "Available";
        public Guid? CurrentSaleSessionId { get; set; }
        public string? Area { get; set; }
        public int? PositionX { get; set; }
        public int? PositionY { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new table.
    /// </summary>
    public class CreateTableSessionDto
    {
        [Required]
        [MaxLength(50)]
        public string TableNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? TableName { get; set; }

        [Required]
        [Range(1, 50)]
        public int Capacity { get; set; }

        public TableShapeDto Shape { get; set; } = TableShapeDto.Rectangle;

        [Range(40, 300)]
        public int Width { get; set; } = 90;

        [Range(40, 300)]
        public int Height { get; set; } = 90;

        [MaxLength(100)]
        public string? Area { get; set; }

        public int? PositionX { get; set; }
        public int? PositionY { get; set; }
    }

    /// <summary>
    /// DTO for updating table information.
    /// </summary>
    public class UpdateTableSessionDto
    {
        [MaxLength(50)]
        public string? TableNumber { get; set; }

        [MaxLength(100)]
        public string? TableName { get; set; }

        [Range(1, 50)]
        public int? Capacity { get; set; }

        public TableShapeDto? Shape { get; set; }

        [Range(40, 300)]
        public int? Width { get; set; }

        [Range(40, 300)]
        public int? Height { get; set; }

        [MaxLength(100)]
        public string? Area { get; set; }

        public int? PositionX { get; set; }
        public int? PositionY { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// DTO for updating table status.
    /// </summary>
    public class UpdateTableStatusDto
    {
        [Required]
        public string Status { get; set; } = "Available";

        public Guid? SaleSessionId { get; set; }
    }

    /// <summary>
    /// DTO for table shape.
    /// </summary>
    public enum TableShapeDto
    {
        Rectangle = 0,
        Circle = 1
    }

}
