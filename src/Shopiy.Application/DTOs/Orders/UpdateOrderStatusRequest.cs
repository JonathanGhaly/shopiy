using Shopiy.Domain.Enums;

namespace Shopiy.Application.DTOs.Orders;

public sealed class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; init; }
}