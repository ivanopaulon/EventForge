using AutoMapper;
using EventForge.Data.Entities.Business;
using EventForge.Data.Entities.Common;
using EventForge.Data.Entities.Documents;
using EventForge.Data.Entities.Events;
using EventForge.Data.Entities.PriceList;
using EventForge.Data.Entities.Products;
using EventForge.Data.Entities.Promotions;
using EventForge.Data.Entities.StationMonitor;
using EventForge.Data.Entities.Store;
using EventForge.Data.Entities.Teams;
using EventForge.Data.Entities.Warehouse;
using EventForge.DTOs.Banks;
using EventForge.DTOs.Business;
using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using EventForge.DTOs.Events;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.Products;
using EventForge.DTOs.Promotions;
using EventForge.DTOs.Station;
using EventForge.DTOs.Store;
using EventForge.DTOs.Teams;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;
using EventForge.DTOs.Warehouse;

namespace EventForge.Mappings;

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

        // Document mappings (if DocumentType exists in DbContext)
        CreateMap<DocumentType, DocumentTypeDto>()
            .ForMember(dest => dest.DefaultWarehouseName, 
                opt => opt.MapFrom(src => src.DefaultWarehouse != null ? src.DefaultWarehouse.Name : null));
        CreateMap<CreateDocumentTypeDto, DocumentType>();
        CreateMap<UpdateDocumentTypeDto, DocumentType>();

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
    }
}