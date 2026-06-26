using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shopiy.Application.DTOs.Categories;
using Shopiy.Application.Features.Categories.Commands.CreateCategory;
using Shopiy.Application.Features.Categories.Commands.DeleteCategory;
using Shopiy.Application.Features.Categories.Commands.UpdateCategory;
using Shopiy.Application.Features.Categories.Queries.GetCategories;
using Shopiy.Application.Features.Categories.Queries.GetCategory;

namespace Shopiy.Api.Controllers;

public sealed class CategoriesController : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCategoriesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{slugOrId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategory(string slugOrId, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new GetCategoryQuery(slugOrId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var result = await Sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetCategory), new { slugOrId = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest("Category ID in route must match the request body ID.");
        }
        var result = await Sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        await Sender.Send(new DeleteCategoryCommand(id), cancellationToken);
        return NoContent();
    }
}
