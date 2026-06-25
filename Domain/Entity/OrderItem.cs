using Shopiy.Domain.Common;

namespace Shopiy.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public int UnitPrice { get; set; }

    public int Total { get; set; }
}