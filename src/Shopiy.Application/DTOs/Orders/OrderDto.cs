using Shopiy.Domain.Enums;

namespace Shopiy.Application.DTOs.Orders;

public sealed class OrderDto
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public OrderStatus Status { get; init; }

    public decimal TotalPrice { get; init; }

    public DateTime CreatedAt { get; init; }

    public IReadOnlyCollection<OrderItemDto> Items { get; init; } = [];
}