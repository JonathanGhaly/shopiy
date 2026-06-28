using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Application.DTOs.Orders;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Orders.Queries.GetOrder;

public sealed class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetOrderHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException(nameof(Order), request.Id);
        }

        var currentUserId = _currentUserService.UserId;
        var isAdmin = _currentUserService.Roles.Contains(Shopiy.Domain.Constants.Roles.Admin);

        if (!isAdmin && order.UserId != currentUserId)
        {
            throw new ForbiddenAccessException();
        }

        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.UserId,
            Status = order.Status,
            TotalPrice = order.Total / 100m,
            CreatedAt = order.CreatedAt,
            UserId = order.UserId,
            Subtotal = order.Subtotal / 100m,
            Tax = order.Tax / 100m,
            Shipping = order.Shipping / 100m,
            Total = order.Total / 100m,
            Currency = order.Currency,
            ShippingAddress = string.IsNullOrWhiteSpace(order.ShippingAddress)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<AddressDto>(order.ShippingAddress, (System.Text.Json.JsonSerializerOptions?)null),
            BillingAddress = string.IsNullOrWhiteSpace(order.BillingAddress)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<AddressDto>(order.BillingAddress, (System.Text.Json.JsonSerializerOptions?)null),
            Notes = order.Notes,
            PlacedAt = order.PlacedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice / 100m
            }).ToList()
        };
    }
}
