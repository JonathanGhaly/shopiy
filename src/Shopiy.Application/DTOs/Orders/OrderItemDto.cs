namespace Shopiy.Application.DTOs.Orders;

public sealed class OrderItemDto
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TotalPrice => Quantity * UnitPrice;
}