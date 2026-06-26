using Shopiy.Domain.Common;
using Shopiy.Domain.Enums;

namespace Shopiy.Domain.Entities;

/// <summary>
/// Represents a customer purchase order.
/// All monetary values (Subtotal, Tax, Shipping, Total) are stored as integers
/// in the smallest currency unit (piastres). Divide by 100 for display.
/// Shipping and billing addresses are serialised as JSON strings.
/// </summary>
public class Order : BaseEntity
{
    /// <summary>Foreign key to <see cref="ApplicationUser"/> who placed the order.</summary>
    public Guid UserId { get; set; }

    /// <summary>Current lifecycle state of the order.</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Sum of all line item totals before tax and shipping (piastres).</summary>
    public int Subtotal { get; set; }

    /// <summary>Calculated VAT amount (14 % Egyptian VAT applied to Subtotal) in piastres.</summary>
    public int Tax { get; set; }

    /// <summary>Fixed shipping fee in piastres (currently 5 000 = 50 EGP).</summary>
    public int Shipping { get; set; }

    /// <summary>Grand total: Subtotal + Tax + Shipping in piastres.</summary>
    public int Total { get; set; }

    /// <summary>ISO 4217 currency code. Defaults to <c>EGP</c>.</summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>JSON-serialised <see cref="Shopiy.Application.DTOs.Orders.AddressDto"/> for the delivery address.</summary>
    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>JSON-serialised <see cref="Shopiy.Application.DTOs.Orders.AddressDto"/> for the billing address.</summary>
    public string BillingAddress { get; set; } = string.Empty;

    /// <summary>Optional free-text note from the customer.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC timestamp when the customer submitted the order.</summary>
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the order was dispatched. <c>null</c> until shipped.</summary>
    public DateTime? ShippedAt { get; set; }

    /// <summary>UTC timestamp when the order was delivered. <c>null</c> until delivered.</summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>The individual product line items belonging to this order.</summary>
    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();
}