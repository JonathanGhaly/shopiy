using MediatR;
using Microsoft.EntityFrameworkCore;
using Shopiy.Application.Common.Exceptions;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Application.Features.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryHandler
    : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        var hasActiveChildren = await _context.Categories
            .AnyAsync(c => c.ParentId == request.Id, cancellationToken);

        if (hasActiveChildren)
        {
            throw new ConflictException("Cannot delete a parent category containing active sub-categories.");
        }

        category.SoftDelete();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
