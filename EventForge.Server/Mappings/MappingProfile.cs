using AutoMapper;
using EventForge.Server.DTOs.Banks;
using EventForge.Server.DTOs.Business;
using EventForge.Server.DTOs.Common;
using EventForge.Server.DTOs.Documents;
using EventForge.Server.DTOs.PriceLists;
using EventForge.Server.DTOs.Products;
using EventForge.Server.DTOs.Promotions;
using EventForge.Server.DTOs.Station;
using EventForge.Server.DTOs.Store;
using EventForge.Server.DTOs.Teams;
using EventForge.Server.DTOs.UnitOfMeasures;
using EventForge.Server.DTOs.VatRates;
using EventForge.Server.DTOs.Warehouse;

namespace EventForge.Server.Mappings;

/// <summary>
/// AutoMapper profile for mapping between entities and DTOs
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Initializes the mapping configurations
    /// </summary>
    public MappingProfile()
    {
        // Bank mappings
        CreateMap<Bank, BankDto>().ReverseMap();
        CreateMap<CreateBankDto, Bank>();
        CreateMap<UpdateBankDto, Bank>();

        // Business mappings
        CreateMap<BusinessParty, BusinessPartyDto>().ReverseMap();
        CreateMap<CreateBusinessPartyDto, BusinessParty>();
        CreateMap<UpdateBusinessPartyDto, BusinessParty>();

        CreateMap<PaymentTerm, PaymentTermDto>().ReverseMap();
        CreateMap<CreatePaymentTermDto, PaymentTerm>();
        CreateMap<UpdatePaymentTermDto, PaymentTerm>();

        // Common mappings
        CreateMap<Address, AddressDto>().ReverseMap();
        CreateMap<CreateAddressDto, Address>();
        CreateMap<UpdateAddressDto, Address>();

        CreateMap<Contact, ContactDto>().ReverseMap();
        CreateMap<CreateContactDto, Contact>();
        CreateMap<UpdateContactDto, Contact>();

        CreateMap<ClassificationNode, ClassificationNodeDto>().ReverseMap();
        CreateMap<CreateClassificationNodeDto, ClassificationNode>();
        CreateMap<UpdateClassificationNodeDto, ClassificationNode>();

        CreateMap<Reference, ReferenceDto>().ReverseMap();
        CreateMap<CreateReferenceDto, Reference>();
        CreateMap<UpdateReferenceDto, Reference>();

        CreateMap<UM, UMDto>().ReverseMap();
        CreateMap<CreateUMDto, UM>();
        CreateMap<UpdateUMDto, UM>();

        CreateMap<VatRate, VatRateDto>().ReverseMap();
        CreateMap<CreateVatRateDto, VatRate>();
        CreateMap<UpdateVatRateDto, VatRate>();

        // Document mappings
        CreateMap<DocumentType, DocumentTypeDto>()
            .ForMember(dest => dest.DefaultWarehouseName,
                opt => opt.MapFrom(src => src.DefaultWarehouse != null ? src.DefaultWarehouse.Name : null));
        CreateMap<CreateDocumentTypeDto, DocumentType>();
        CreateMap<UpdateDocumentTypeDto, DocumentType>();

        CreateMap<DocumentHeader, DocumentHeaderDto>()
            .ForMember(dest => dest.DocumentTypeName,
                opt => opt.MapFrom(src => src.DocumentType != null ? src.DocumentType.Name : null))
            .ForMember(dest => dest.BusinessPartyName,
                opt => opt.MapFrom(src => src.BusinessParty != null ? src.BusinessParty.Name : null))
            .ForMember(dest => dest.SourceWarehouseName,
                opt => opt.MapFrom(src => src.SourceWarehouse != null ? src.SourceWarehouse.Name : null))
            .ForMember(dest => dest.DestinationWarehouseName,
                opt => opt.MapFrom(src => src.DestinationWarehouse != null ? src.DestinationWarehouse.Name : null))
            .ForMember(dest => dest.TeamMemberName,
                opt => opt.MapFrom(src => src.TeamMember != null ? $"{src.TeamMember.FirstName} {src.TeamMember.LastName}" : null))
            .ForMember(dest => dest.TeamName,
                opt => opt.MapFrom(src => src.Team != null ? src.Team.Name : null))
            .ForMember(dest => dest.EventName,
                opt => opt.MapFrom(src => src.Event != null ? src.Event.Name : null))
            .ForMember(dest => dest.CashierName,
                opt => opt.MapFrom(src => src.Cashier != null ? src.Cashier.Name : null))
            .ForMember(dest => dest.TotalBeforeDiscount,
                opt => opt.MapFrom(src => src.TotalBeforeDiscount))
            .ForMember(dest => dest.TotalAfterDiscount,
                opt => opt.MapFrom(src => src.TotalAfterDiscount));
        CreateMap<CreateDocumentHeaderDto, DocumentHeader>();
        CreateMap<UpdateDocumentHeaderDto, DocumentHeader>();

        CreateMap<DocumentRow, DocumentRowDto>()
            .ForMember(dest => dest.SourceWarehouseName,
                opt => opt.MapFrom(src => src.SourceWarehouse != null ? src.SourceWarehouse.Name : null))
            .ForMember(dest => dest.DestinationWarehouseName,
                opt => opt.MapFrom(src => src.DestinationWarehouse != null ? src.DestinationWarehouse.Name : null))
            .ForMember(dest => dest.StationName,
                opt => opt.MapFrom(src => src.Station != null ? src.Station.Name : null))
            .ForMember(dest => dest.LineTotal,
                opt => opt.MapFrom(src => src.LineTotal))
            .ForMember(dest => dest.VatTotal,
                opt => opt.MapFrom(src => src.VatTotal))
            .ForMember(dest => dest.DiscountTotal,
                opt => opt.MapFrom(src => src.DiscountTotal));
        CreateMap<CreateDocumentRowDto, DocumentRow>();
        CreateMap<UpdateDocumentRowDto, DocumentRow>();

        CreateMap<DocumentSummaryLink, DocumentSummaryLinkDto>()
            .ForMember(dest => dest.SummaryDocumentNumber,
                opt => opt.MapFrom(src => src.SummaryDocument != null ? src.SummaryDocument.Number : null))
            .ForMember(dest => dest.SummaryDocumentDate,
                opt => opt.MapFrom(src => src.SummaryDocument != null ? src.SummaryDocument.Date : (DateTime?)null))
            .ForMember(dest => dest.DetailedDocumentNumber,
                opt => opt.MapFrom(src => src.DetailedDocument != null ? src.DetailedDocument.Number : null))
            .ForMember(dest => dest.DetailedDocumentDate,
                opt => opt.MapFrom(src => src.DetailedDocument != null ? src.DetailedDocument.Date : (DateTime?)null));
        CreateMap<CreateDocumentSummaryLinkDto, DocumentSummaryLink>();

        // Event mappings
        CreateMap<Event, EventDto>().ReverseMap();
        CreateMap<CreateEventDto, Event>();
        CreateMap<UpdateEventDto, Event>();

        // Team mappings
        CreateMap<Team, TeamDto>().ReverseMap();
        CreateMap<CreateTeamDto, Team>();
        CreateMap<UpdateTeamDto, Team>();

        CreateMap<TeamMember, TeamMemberDto>().ReverseMap();
        CreateMap<CreateTeamMemberDto, TeamMember>();
        CreateMap<UpdateTeamMemberDto, TeamMember>();

        // Product mappings
        CreateMap<Product, ProductDto>().ReverseMap();
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        // PriceList mappings
        CreateMap<PriceList, PriceListDto>().ReverseMap();
        CreateMap<CreatePriceListDto, PriceList>();
        CreateMap<UpdatePriceListDto, PriceList>();

        CreateMap<PriceListEntry, PriceListEntryDto>().ReverseMap();
        CreateMap<CreatePriceListEntryDto, PriceListEntry>();
        CreateMap<UpdatePriceListEntryDto, PriceListEntry>();

        // Promotion mappings
        CreateMap<Promotion, PromotionDto>().ReverseMap();
        CreateMap<CreatePromotionDto, Promotion>();
        CreateMap<UpdatePromotionDto, Promotion>();

        // Station mappings
        CreateMap<Station, StationDto>().ReverseMap();
        CreateMap<CreateStationDto, Station>();
        CreateMap<UpdateStationDto, Station>();

        // Store mappings
        CreateMap<StoreUser, StoreUserDto>().ReverseMap();
        CreateMap<CreateStoreUserDto, StoreUser>();
        CreateMap<UpdateStoreUserDto, StoreUser>();

        CreateMap<StoreUserGroup, StoreUserGroupDto>().ReverseMap();
        CreateMap<CreateStoreUserGroupDto, StoreUserGroup>();
        CreateMap<UpdateStoreUserGroupDto, StoreUserGroup>();

        CreateMap<StoreUserPrivilege, StoreUserPrivilegeDto>().ReverseMap();
        CreateMap<CreateStoreUserPrivilegeDto, StoreUserPrivilege>();
        CreateMap<UpdateStoreUserPrivilegeDto, StoreUserPrivilege>();

        // Warehouse mappings
        CreateMap<StorageFacility, StorageFacilityDto>().ReverseMap();
        CreateMap<CreateStorageFacilityDto, StorageFacility>();
        CreateMap<UpdateStorageFacilityDto, StorageFacility>();

        CreateMap<StorageLocation, StorageLocationDto>().ReverseMap();
        CreateMap<CreateStorageLocationDto, StorageLocation>();
        CreateMap<UpdateStorageLocationDto, StorageLocation>();

        // Authentication mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore());

        // Tenant mappings
        CreateMap<Tenant, TenantResponseDto>();
        CreateMap<CreateTenantDto, Tenant>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.AdminTenants, opt => opt.Ignore());
        
        CreateMap<UpdateTenantDto, Tenant>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ForMember(dest => dest.IsEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionExpiresAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.AdminTenants, opt => opt.Ignore());

        CreateMap<AdminTenant, AdminTenantResponseDto>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.ManagedTenant.Name));

        CreateMap<AuditTrail, AuditTrailResponseDto>()
            .ForMember(dest => dest.PerformedByUsername, opt => opt.MapFrom(src => src.PerformedByUser.Username))
            .ForMember(dest => dest.SourceTenantName, opt => opt.MapFrom(src => src.SourceTenant != null ? src.SourceTenant.Name : null))
            .ForMember(dest => dest.TargetTenantName, opt => opt.MapFrom(src => src.TargetTenant != null ? src.TargetTenant.Name : null))
            .ForMember(dest => dest.TargetUsername, opt => opt.MapFrom(src => src.TargetUser != null ? src.TargetUser.Username : null));
    }
}