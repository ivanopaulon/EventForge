namespace EventForge.Client.Services.Schema
{
    /// <summary>
    /// Data type classification for fields.
    /// </summary>
    public enum FieldDataType
    {
        String,
        Integer,
        Decimal,
        Boolean,
        DateTime,
        Guid,
        Enum,
        Unknown
    }

    /// <summary>
    /// Metadata describing a field/property in an entity.
    /// </summary>
    public class FieldMetadata
    {
        /// <summary>
        /// Name of the field/property.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full path for nested fields (e.g., "Product.Category.Name").
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Data type of the field.
        /// </summary>
        public FieldDataType DataType { get; set; }

        /// <summary>
        /// Underlying C# type.
        /// </summary>
        public Type ClrType { get; set; } = typeof(object);

        /// <summary>
        /// Whether the field is nullable.
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// User-friendly description of the field.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Example values for the field.
        /// </summary>
        public List<string> Examples { get; set; } = new();

        /// <summary>
        /// Whether this field can be used for numeric calculations.
        /// </summary>
        public bool IsNumeric => DataType == FieldDataType.Integer || DataType == FieldDataType.Decimal;

        /// <summary>
        /// Whether this field can be used for aggregations.
        /// </summary>
        public bool IsAggregatable => IsNumeric;
    }

    /// <summary>
    /// Provides schema information for entities.
    /// </summary>
    public interface IEntitySchemaProvider
    {
        /// <summary>
        /// Gets all available fields for the specified entity type.
        /// </summary>
        /// <param name="entityType">The entity type name (e.g., "VatRate", "Product").</param>
        /// <param name="includeNested">Whether to include nested object properties.</param>
        /// <returns>List of field metadata.</returns>
        List<FieldMetadata> GetAvailableFields(string entityType, bool includeNested = false);

        /// <summary>
        /// Gets fields that are compatible with the specified metric type.
        /// </summary>
        /// <param name="entityType">The entity type name.</param>
        /// <param name="metricType">The metric type to filter by.</param>
        /// <param name="includeNested">Whether to include nested object properties.</param>
        /// <returns>List of compatible field metadata.</returns>
        List<FieldMetadata> GetCompatibleFields(string entityType, EventForge.DTOs.Dashboard.MetricType metricType, bool includeNested = false);

        /// <summary>
        /// Gets a specific field by its path.
        /// </summary>
        /// <param name="entityType">The entity type name.</param>
        /// <param name="fieldPath">The field path (e.g., "Percentage" or "Product.Name").</param>
        /// <returns>Field metadata or null if not found.</returns>
        FieldMetadata? GetField(string entityType, string fieldPath);

        /// <summary>
        /// Gets all supported entity types.
        /// </summary>
        /// <returns>List of entity type names.</returns>
        List<string> GetSupportedEntityTypes();
    }
}
