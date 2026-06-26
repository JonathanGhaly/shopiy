using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Enums;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusHandler
    : IRequestHandler<UpdateOrderStatusCommand, UpdateOrderStatusResult>
{
    private readonly IApplicationDbContext _context;

    public UpdateOrderStatusHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateOrderStatusResult> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.NewStatus, ignoreCase: true, out var newStatus))
        {
            throw new Shopiy.Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure("status",
                    "Invalid status value. Allowed: pending, paid, shipped, delivered, cancelled, refunded.") });
        }

        // Load order with items (for stock deduction when moving to Paid)
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order is null)
            throw new NotFoundException(nameof(Order), request.OrderId);

        // Business Rule: Cannot cancel after Shipped or Delivered
        if (newStatus == OrderStatus.Cancelled &&
            (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered))
        {
            throw new ConflictException(
                $"Cannot cancel an order that has already been {order.Status.ToString().ToLower()}.");
        }

        var now = DateTime.UtcNow;

        // Business Rule: Transition to Paid → atomically decrement stock
        if (newStatus == OrderStatus.Paid && order.Status != OrderStatus.Paid)
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();

            // Use row-level locking to prevent race conditions
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product is null)
                    throw new NotFoundException("Product", item.ProductId);

                if (product.StockQuantity < item.Quantity)
                    throw new ConflictException(
                        $"Insufficient stock for product '{product.Name}' to mark order as paid. " +
                        $"Available: {product.StockQuantity}, required: {item.Quantity}.");

                product.StockQuantity -= item.Quantity;
            }
        }

        // Transition to Shipped
        if (newStatus == OrderStatus.Shipped && order.Status != OrderStatus.Shipped)
            order.ShippedAt = now;

        // Transition to Delivered
        if (newStatus == OrderStatus.Delivered && order.Status != OrderStatus.Delivered)
            order.DeliveredAt = now;

        order.Status = newStatus;
        order.MarkUpdated();

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateOrderStatusResult
        {
            Message = "Order status successfully updated.",
            OrderId = order.Id,
            NewStatus = order.Status.ToString().ToLower(),
            UpdatedAt = now
        };
    }
}
