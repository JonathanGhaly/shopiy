using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shopiy.Application.DTOs.Orders;
using Shopiy.Application.Features.Orders.Commands.CreateOrder;
using Shopiy.Application.Features.Orders.Commands.UpdateOrderStatus;
using Shopiy.Application.Features.Orders.Queries.GetOrder;
using Shopiy.Application.Features.Orders.Queries.GetOrders;

namespace Shopiy.Api.Controllers;

[Authorize]
public sealed class OrdersController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOrdersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetOrderQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var command = new CreateOrderCommand(
            CurrentUserId.Value,
            request.Items,
            request.ShippingAddress,
            request.BillingAddress,
            request.Notes
        );
        var result = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new UpdateOrderStatusCommand(id, request.Status.ToString()), cancellationToken);
        return Ok(result);
    }
}
