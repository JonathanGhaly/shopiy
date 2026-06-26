using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Application.DTOs.Orders;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Enums;
using Shopiy.Domain.Interfaces;
using System.Text.Json;

namespace Shopiy.Application.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderHandler
    : IRequestHandler<CreateOrderCommand, OrderConfirmationDto>
{
    private readonly IApplicationDbContext _context;

    // Tax rate: 14% (Egyptian VAT)
    private const decimal TaxRate = 0.14m;
    // Fixed shipping: 50 EGP = 5000 piastres
    private const int ShippingFee = 5000;

    public CreateOrderHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderConfirmationDto> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Items.Any())
            throw new Shopiy.Application.Common.Exceptions.ValidationException(
                new[] { new FluentValidation.Results.ValidationFailure("Items", "Order must contain at least one item.") });

        var productIds = request.Items.Select(i => i.ProductId).ToList();

        // Load products using DATABASE prices (never trust client totals)
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        // Validate all products exist and have stock
        foreach (var item in request.Items)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new NotFoundException("Product", item.ProductId);

            if (product.StockQuantity < item.Quantity)
                throw new ConflictException($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}, requested: {item.Quantity}.");
        }

        // Calculate totals using DB prices
        var orderItems = new List<OrderItem>();
        var subtotal = 0;

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            var lineTotal = product.Price * item.Quantity;
            subtotal += lineTotal;

            orderItems.Add(new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                Total = lineTotal
            });
        }

        var tax = (int)(subtotal * TaxRate);
        var shipping = ShippingFee;
        var total = subtotal + tax + shipping;

        var shippingJson = JsonSerializer.Serialize(request.ShippingAddress);
        var billingJson = JsonSerializer.Serialize(request.BillingAddress);

        var order = new Order
        {
            UserId = request.UserId,
            Status = OrderStatus.Pending,
            Subtotal = subtotal,
            Tax = tax,
            Shipping = shipping,
            Total = total,
            ShippingAddress = shippingJson,
            BillingAddress = billingJson,
            Notes = request.Notes,
            PlacedAt = DateTime.UtcNow,
            Items = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return new OrderConfirmationDto
        {
            OrderId = order.Id,
            Status = order.Status.ToString().ToLower(),
            Subtotal = order.Subtotal,
            Tax = order.Tax,
            Shipping = order.Shipping,
            Total = order.Total,
            Currency = order.Currency,
            PlacedAt = order.PlacedAt
        };
    }
}
