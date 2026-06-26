using MediatR;

namespace Shopiy.Application.Features.Orders.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(Guid OrderId, string NewStatus) : IRequest<UpdateOrderStatusResult>;

public class UpdateOrderStatusResult
{
    public string Message { get; init; } = string.Empty;
    public Guid OrderId { get; init; }
    public string NewStatus { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}
