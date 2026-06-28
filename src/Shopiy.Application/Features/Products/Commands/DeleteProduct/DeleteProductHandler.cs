using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductHandler
    : IRequestHandler<DeleteProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public DeleteProductHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.Id);
        }

        product.IsActive = false;
        product.SoftDelete();

        await _context.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByPrefixAsync("Shopiy.Application.Features.Products.Queries.GetProducts.GetProductsQuery", cancellationToken);
        await _cache.RemoveByPrefixAsync("Shopiy.Application.Features.Products.Queries.GetProduct.GetProductQuery", cancellationToken);
    }
}
