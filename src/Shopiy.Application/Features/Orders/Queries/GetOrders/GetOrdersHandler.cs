using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.DTOs.Orders;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Orders.Queries.GetOrders;

public sealed class GetOrdersHandler : IRequestHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrdersHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var isAdmin = _currentUserService.Roles.Contains(Shopiy.Domain.Constants.Roles.Admin);

        var query = _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking();

        if (!isAdmin)
        {
            query = query.Where(o => o.UserId == currentUserId);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            CustomerId = o.UserId,
            Status = o.Status,
            TotalPrice = o.Total / 100m,
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice / 100m
            }).ToList()
        }).ToList();
    }
}
