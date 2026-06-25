using Shopiy.Domain.Common;
using Shopiy.Domain.Enums;

namespace Shopiy.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public int Subtotal { get; set; }

    public int Tax { get; set; }

    public int Shipping { get; set; }

    public int Total { get; set; }

    public string Currency { get; set; } = "EGP";

    public string ShippingAddress { get; set; } = string.Empty;

    public string BillingAddress { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
}