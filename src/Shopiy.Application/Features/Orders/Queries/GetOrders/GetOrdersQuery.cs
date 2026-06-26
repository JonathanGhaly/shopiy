using MediatR;
using Shopiy.Application.DTOs.Orders;

namespace Shopiy.Application.Features.Orders.Queries.GetOrders;

public record GetOrdersQuery : IRequest<IEnumerable<OrderDto>>;
