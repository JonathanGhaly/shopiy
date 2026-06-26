using MediatR;

namespace Shopiy.Application.Features.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(
    Guid Id
) : IRequest;
