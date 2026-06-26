using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shopiy.Application.DTOs.Products;
using Shopiy.Application.Features.Products.Commands.CreateProduct;
using Shopiy.Application.Features.Products.Commands.DeleteProduct;
using Shopiy.Application.Features.Products.Commands.UpdateProduct;
using Shopiy.Application.Features.Products.Queries.GetProduct;
using Shopiy.Application.Features.Products.Queries.GetProducts;

namespace Shopiy.Api.Controllers;

public sealed class ProductsController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? sort = null,
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Sender.Send(new GetProductsQuery(page, limit, sort, categoryId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{slugOrId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(string slugOrId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetProductQuery(slugOrId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.CategoryIds
        );
        var result = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { slugOrId = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            id,
            request.Name,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.CategoryIds
        );
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }
}
