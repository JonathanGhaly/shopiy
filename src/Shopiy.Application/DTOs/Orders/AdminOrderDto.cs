namespace Shopiy.Application.DTOs.Orders;

public sealed class AdminOrderDto
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Total { get; init; }
    public string Currency { get; init; } = "EGP";
    public DateTime PlacedAt { get; init; }
}
