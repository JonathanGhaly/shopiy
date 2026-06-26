using MediatR;
using Shopiy.Application.DTOs.Orders;

namespace Shopiy.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(
    Guid UserId,
    ICollection<OrderItemRequest> Items,
    AddressDto ShippingAddress,
    AddressDto BillingAddress,
    string? Notes
) : IRequest<OrderConfirmationDto>;
