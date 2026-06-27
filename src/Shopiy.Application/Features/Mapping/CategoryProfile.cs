using AutoMapper;
using Shopiy.Application.DTOs.Categories;
using Shopiy.Domain.Entities;

namespace Shopiy.Application.Mapping;

public sealed class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();
    }
}
