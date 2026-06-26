using MediatR;

namespace Shopiy.Application.Features.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(
    Guid Id
) : IRequest;