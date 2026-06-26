using AutoMapper;
using Shopiy.Application.DTOs.Products;
using Shopiy.Domain.Entities;

namespace Shopiy.Application.Mapping;

public sealed class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price / 100m))
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src =>
                src.ProductCategories.Select(pc => pc.Category != null ? pc.Category.Name : string.Empty).ToList()));
    }
}
