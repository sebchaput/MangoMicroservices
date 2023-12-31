using AutoMapper;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.Models.Dto;

namespace Mango.Services.OrderAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<CartHeaderDto, OrderHeaderDto>()
                    .ForMember(dest => dest.OrderTotal, u => u.MapFrom(src => src.CartTotal))
                    .ReverseMap();
                config.CreateMap<CartDetailsDto, OrderDetailsDto>()
                    .ForMember(dest => dest.ProductName, u => u.MapFrom(src => src.Product.Name))
                    .ForMember(dest => dest.Price, u => u.MapFrom(src => src.Product.Price));

                config.CreateMap<OrderDetailsDto, CartDetailsDto>();

                config.CreateMap<OrderHeader, OrderHeaderDto>().ReverseMap();
                config.CreateMap<OrderDetails, OrderDetailsDto>().ReverseMap();
            });
            return mappingConfig;
        }
    }
}
