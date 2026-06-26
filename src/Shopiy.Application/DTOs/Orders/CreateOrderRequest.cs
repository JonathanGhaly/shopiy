namespace Shopiy.Application.DTOs.Orders;

public sealed class CreateOrderRequest
{
    public ICollection<OrderItemRequest> Items { get; init; } = [];
}

public sealed class OrderItemRequest
{
    public Guid ProductId { get; init; }

    public int Quantity { get; init; }
}