namespace Shopiy.Application.DTOs.Orders;

public sealed class CreateOrderRequest
{
    public ICollection<OrderItemRequest> Items { get; init; } = [];

    public AddressDto ShippingAddress { get; init; } = null!;

    public AddressDto BillingAddress { get; init; } = null!;

    public string? Notes { get; init; }
}

public sealed class OrderItemRequest
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}