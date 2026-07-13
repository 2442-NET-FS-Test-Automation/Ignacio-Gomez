using AutoMapper;
using Library.ControllerApi.DTOs;
using Library.Data.Entities;

namespace Library.ControllerApi.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<InventoryItem, InventoryDto>()
            .ForCtorParam("Sku", o => o.MapFrom(s => s.Product.Sku))
            .ForCtorParam("Name", o => o.MapFrom(s => s.Product.Name));
        

        
        //CreateMap<inventoryItem, InventoryDto>
    }
}