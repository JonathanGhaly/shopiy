using AutoMapper;
using Shopiy.Application.DTOs.Products;
using Shopiy.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Shopiy.Application.Mapping;

public sealed class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price / 100m))
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src =>
                src.ProductCategories.Select(pc => pc.Category != null ? pc.Category.Name : string.Empty).ToList()))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.Metadata)
                    ? new Dictionary<string, object>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(src.Metadata, (System.Text.Json.JsonSerializerOptions?)null)));
    }
}
