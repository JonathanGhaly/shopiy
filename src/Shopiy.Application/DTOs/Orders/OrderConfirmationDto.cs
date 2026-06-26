namespace Shopiy.Application.DTOs.Orders;

public sealed class OrderConfirmationDto
{
    public Guid OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int Subtotal { get; init; }
    public int Tax { get; init; }
    public int Shipping { get; init; }
    public int Total { get; init; }
    public string Currency { get; init; } = "EGP";
    public DateTime PlacedAt { get; init; }
}
