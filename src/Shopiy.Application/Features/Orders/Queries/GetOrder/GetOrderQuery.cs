using MediatR;
using Shopiy.Application.DTOs.Orders;

namespace Shopiy.Application.Features.Orders.Queries.GetOrder;

public record GetOrderQuery(Guid Id) : IRequest<OrderDto>;
