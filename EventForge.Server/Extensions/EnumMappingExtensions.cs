using EventForge.DTOs.Common;
using EventForge.Server.Data.Entities.Common;

namespace EventForge.Server.Extensions
{
    /// <summary>
    /// Extension methods to convert between entity enums and DTO enums.
    /// </summary>
    public static class EnumMappingExtensions
    {
        /// <summary>
        /// Convert entity AddressType to DTO AddressType.
        /// </summary>
        public static DTOs.Common.AddressType ToDto(this Data.Entities.Common.AddressType entityEnum)
        {
            return entityEnum switch
            {
                Data.Entities.Common.AddressType.Legal => DTOs.Common.AddressType.Legal,
                Data.Entities.Common.AddressType.Operational => DTOs.Common.AddressType.Operational,
                Data.Entities.Common.AddressType.Destination => DTOs.Common.AddressType.Destination,
                _ => DTOs.Common.AddressType.Operational
            };
        }

        /// <summary>
        /// Convert DTO AddressType to entity AddressType.
        /// </summary>
        public static Data.Entities.Common.AddressType ToEntity(this DTOs.Common.AddressType dtoEnum)
        {
            return dtoEnum switch
            {
                DTOs.Common.AddressType.Legal => Data.Entities.Common.AddressType.Legal,
                DTOs.Common.AddressType.Operational => Data.Entities.Common.AddressType.Operational,
                DTOs.Common.AddressType.Destination => Data.Entities.Common.AddressType.Destination,
                _ => Data.Entities.Common.AddressType.Operational
            };
        }

        /// <summary>
        /// Convert entity ContactType to DTO ContactType.
        /// </summary>
        public static DTOs.Common.ContactType ToDto(this Data.Entities.Common.ContactType entityEnum)
        {
            return entityEnum switch
            {
                Data.Entities.Common.ContactType.Email => DTOs.Common.ContactType.Email,
                Data.Entities.Common.ContactType.Phone => DTOs.Common.ContactType.Phone,
                Data.Entities.Common.ContactType.Fax => DTOs.Common.ContactType.Fax,
                Data.Entities.Common.ContactType.PEC => DTOs.Common.ContactType.PEC,
                Data.Entities.Common.ContactType.Other => DTOs.Common.ContactType.Other,
                _ => DTOs.Common.ContactType.Other
            };
        }

        /// <summary>
        /// Convert DTO ContactType to entity ContactType.
        /// </summary>
        public static Data.Entities.Common.ContactType ToEntity(this DTOs.Common.ContactType dtoEnum)
        {
            return dtoEnum switch
            {
                DTOs.Common.ContactType.Email => Data.Entities.Common.ContactType.Email,
                DTOs.Common.ContactType.Phone => Data.Entities.Common.ContactType.Phone,
                DTOs.Common.ContactType.Fax => Data.Entities.Common.ContactType.Fax,
                DTOs.Common.ContactType.PEC => Data.Entities.Common.ContactType.PEC,
                DTOs.Common.ContactType.Other => Data.Entities.Common.ContactType.Other,
                _ => Data.Entities.Common.ContactType.Other
            };
        }

        /// <summary>
        /// Convert entity ProductClassificationType to DTO ProductClassificationType.
        /// </summary>
        public static DTOs.Common.ProductClassificationType ToDto(this Data.Entities.Common.ProductClassificationType entityEnum)
        {
            return entityEnum switch
            {
                Data.Entities.Common.ProductClassificationType.Category => DTOs.Common.ProductClassificationType.Category,
                Data.Entities.Common.ProductClassificationType.Subcategory => DTOs.Common.ProductClassificationType.Subcategory,
                Data.Entities.Common.ProductClassificationType.Brand => DTOs.Common.ProductClassificationType.Brand,
                Data.Entities.Common.ProductClassificationType.Line => DTOs.Common.ProductClassificationType.Line,
                _ => DTOs.Common.ProductClassificationType.Category
            };
        }

        /// <summary>
        /// Convert DTO ProductClassificationType to entity ProductClassificationType.
        /// </summary>
        public static Data.Entities.Common.ProductClassificationType ToEntity(this DTOs.Common.ProductClassificationType dtoEnum)
        {
            return dtoEnum switch
            {
                DTOs.Common.ProductClassificationType.Category => Data.Entities.Common.ProductClassificationType.Category,
                DTOs.Common.ProductClassificationType.Subcategory => Data.Entities.Common.ProductClassificationType.Subcategory,
                DTOs.Common.ProductClassificationType.Brand => Data.Entities.Common.ProductClassificationType.Brand,
                DTOs.Common.ProductClassificationType.Line => Data.Entities.Common.ProductClassificationType.Line,
                _ => Data.Entities.Common.ProductClassificationType.Category
            };
        }

        /// <summary>
        /// Convert entity ProductClassificationNodeStatus to DTO ProductClassificationNodeStatus.
        /// </summary>
        public static DTOs.Common.ProductClassificationNodeStatus ToDto(this Data.Entities.Common.ProductClassificationNodeStatus entityEnum)
        {
            return entityEnum switch
            {
                Data.Entities.Common.ProductClassificationNodeStatus.Active => DTOs.Common.ProductClassificationNodeStatus.Active,
                Data.Entities.Common.ProductClassificationNodeStatus.Inactive => DTOs.Common.ProductClassificationNodeStatus.Inactive,
                Data.Entities.Common.ProductClassificationNodeStatus.Pending => DTOs.Common.ProductClassificationNodeStatus.Pending,
                _ => DTOs.Common.ProductClassificationNodeStatus.Active
            };
        }

        /// <summary>
        /// Convert DTO ProductClassificationNodeStatus to entity ProductClassificationNodeStatus.
        /// </summary>
        public static Data.Entities.Common.ProductClassificationNodeStatus ToEntity(this DTOs.Common.ProductClassificationNodeStatus dtoEnum)
        {
            return dtoEnum switch
            {
                DTOs.Common.ProductClassificationNodeStatus.Active => Data.Entities.Common.ProductClassificationNodeStatus.Active,
                DTOs.Common.ProductClassificationNodeStatus.Inactive => Data.Entities.Common.ProductClassificationNodeStatus.Inactive,
                DTOs.Common.ProductClassificationNodeStatus.Pending => Data.Entities.Common.ProductClassificationNodeStatus.Pending,
                _ => Data.Entities.Common.ProductClassificationNodeStatus.Active
            };
        }
    }
}