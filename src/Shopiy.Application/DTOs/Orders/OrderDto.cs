using Shopiy.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Shopiy.Application.DTOs.Orders;

public sealed class OrderDto
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public OrderStatus Status { get; init; }

    public decimal TotalPrice { get; init; }

    public DateTime CreatedAt { get; init; }

    // Frontend Alignment Fields
    public Guid UserId { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Tax { get; init; }
    public decimal Shipping { get; init; }
    public decimal Total { get; init; }
    public string Currency { get; init; } = "EGP";
    public AddressDto? ShippingAddress { get; init; }
    public AddressDto? BillingAddress { get; init; }
    public string? Notes { get; init; }
    public DateTime PlacedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }

    public IReadOnlyCollection<OrderItemDto> Items { get; init; } = [];
}