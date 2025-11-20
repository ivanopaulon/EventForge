using EventForge.DTOs.Dashboard;
using System.Reflection;

namespace EventForge.Client.Services.Schema
{
    /// <summary>
    /// Provides entity schema information using reflection.
    /// </summary>
    public class EntitySchemaProvider : IEntitySchemaProvider
    {
        private readonly Dictionary<string, Type> _entityTypeMap = new()
        {
            { "VatRate", typeof(EventForge.DTOs.VatRates.VatRateDto) },
            { "Product", typeof(EventForge.DTOs.Products.ProductDto) },
            { "BusinessParty", typeof(EventForge.DTOs.Business.BusinessPartyDto) },
        };

        public List<FieldMetadata> GetAvailableFields(string entityType, bool includeNested = false)
        {
            if (!_entityTypeMap.TryGetValue(entityType, out var dtoType))
            {
                return new List<FieldMetadata>();
            }

            return ExtractFields(dtoType, string.Empty, includeNested);
        }

        public List<FieldMetadata> GetCompatibleFields(string entityType, MetricType metricType, bool includeNested = false)
        {
            var allFields = GetAvailableFields(entityType, includeNested);

            // Count doesn't need a field
            if (metricType == MetricType.Count)
            {
                return new List<FieldMetadata>();
            }

            // Sum, Average, Min, Max require numeric fields
            return allFields.Where(f => f.IsNumeric).ToList();
        }

        public FieldMetadata? GetField(string entityType, string fieldPath)
        {
            var allFields = GetAvailableFields(entityType, true);
            return allFields.FirstOrDefault(f => f.Path == fieldPath);
        }

        public List<string> GetSupportedEntityTypes()
        {
            return _entityTypeMap.Keys.ToList();
        }

        private List<FieldMetadata> ExtractFields(Type type, string prefix, bool includeNested, int depth = 0)
        {
            var fields = new List<FieldMetadata>();
            const int maxDepth = 2; // Prevent infinite recursion

            if (depth > maxDepth)
            {
                return fields;
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead);

            foreach (var prop in properties)
            {
                var fieldName = prop.Name;
                var fieldPath = string.IsNullOrEmpty(prefix) ? fieldName : $"{prefix}.{fieldName}";

                // Skip computed/navigation properties that are too complex
                if (IsComplexNavigationProperty(prop))
                {
                    continue;
                }

                var dataType = GetDataType(prop.PropertyType);
                var metadata = new FieldMetadata
                {
                    Name = fieldName,
                    Path = fieldPath,
                    DataType = dataType,
                    ClrType = prop.PropertyType,
                    IsNullable = IsNullableType(prop.PropertyType),
                    Description = GetFieldDescription(fieldName, dataType),
                    Examples = GetFieldExamples(fieldName, dataType)
                };

                fields.Add(metadata);

                // Recursively add nested fields if requested
                if (includeNested && IsNavigableType(prop.PropertyType) && depth < maxDepth)
                {
                    var nestedType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    fields.AddRange(ExtractFields(nestedType, fieldPath, true, depth + 1));
                }
            }

            return fields;
        }

        private FieldDataType GetDataType(Type type)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(string))
                return FieldDataType.String;
            if (underlyingType == typeof(int) || underlyingType == typeof(long) || underlyingType == typeof(short))
                return FieldDataType.Integer;
            if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float))
                return FieldDataType.Decimal;
            if (underlyingType == typeof(bool))
                return FieldDataType.Boolean;
            if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
                return FieldDataType.DateTime;
            if (underlyingType == typeof(Guid))
                return FieldDataType.Guid;
            if (underlyingType.IsEnum)
                return FieldDataType.Enum;

            return FieldDataType.Unknown;
        }

        private bool IsNullableType(Type type)
        {
            if (!type.IsValueType)
                return true; // Reference types are nullable

            return Nullable.GetUnderlyingType(type) != null;
        }

        private bool IsComplexNavigationProperty(PropertyInfo prop)
        {
            // Skip collections and complex navigation properties
            var propType = prop.PropertyType;

            if (propType.IsGenericType)
            {
                var genericTypeDef = propType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(List<>) ||
                    genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(IEnumerable<>))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsNavigableType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            // Only navigate into custom DTO types, not primitives
            if (underlyingType.IsPrimitive ||
                underlyingType == typeof(string) ||
                underlyingType == typeof(DateTime) ||
                underlyingType == typeof(DateTimeOffset) ||
                underlyingType == typeof(Guid) ||
                underlyingType == typeof(decimal) ||
                underlyingType.IsEnum)
            {
                return false;
            }

            return true;
        }

        private string GetFieldDescription(string fieldName, FieldDataType dataType)
        {
            // Provide user-friendly descriptions based on common field names
            return fieldName switch
            {
                "Percentage" => "Percentuale IVA",
                "Name" => "Nome",
                "Status" => "Stato",
                "Amount" => "Importo",
                "Quantity" => "Quantità",
                "Price" => "Prezzo",
                "DefaultPrice" => "Prezzo predefinito",
                "IsActive" => "Attivo",
                "IsVatIncluded" => "IVA inclusa",
                "CreatedAt" => "Data creazione",
                "ModifiedAt" => "Data modifica",
                "ValidFrom" => "Valido da",
                "ValidTo" => "Valido fino",
                "Code" => "Codice",
                "Description" => "Descrizione",
                "ReorderPoint" => "Punto di riordino",
                "SafetyStock" => "Scorta di sicurezza",
                "TargetStockLevel" => "Livello scorte target",
                "AverageDailyDemand" => "Domanda media giornaliera",
                _ => $"Campo {fieldName} di tipo {dataType}"
            };
        }

        private List<string> GetFieldExamples(string fieldName, FieldDataType dataType)
        {
            // Provide realistic examples based on field name and type
            return (fieldName, dataType) switch
            {
                ("Percentage", FieldDataType.Decimal) => new List<string> { "22.0", "10.0", "4.0" },
                ("Name", FieldDataType.String) => new List<string> { "IVA Ordinaria", "Prodotto A" },
                ("Status", _) => new List<string> { "Active", "Inactive" },
                ("Amount", FieldDataType.Decimal) => new List<string> { "1000.00", "500.50" },
                ("Quantity", _) => new List<string> { "10", "50", "100" },
                ("Price", FieldDataType.Decimal) => new List<string> { "99.99", "149.90" },
                ("DefaultPrice", FieldDataType.Decimal) => new List<string> { "99.99", "149.90" },
                ("IsActive", FieldDataType.Boolean) => new List<string> { "true", "false" },
                ("IsVatIncluded", FieldDataType.Boolean) => new List<string> { "true", "false" },
                ("Code", FieldDataType.String) => new List<string> { "PROD001", "SKU123" },
                _ => new List<string>()
            };
        }
    }
}
