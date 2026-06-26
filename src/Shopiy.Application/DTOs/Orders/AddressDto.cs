namespace Shopiy.Application.DTOs.Orders;

public sealed class AddressDto
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string? PostalCode { get; init; }
    public string Country { get; init; } = string.Empty;
}
