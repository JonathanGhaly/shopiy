using FluentValidation;

namespace Shopiy.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .Length(3, 300).WithMessage("Product name must be between 3 and 300 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0.")
            .Must(p => p % 1 == 0).WithMessage("Price must be submitted in cents/piasters as a whole number.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100).WithMessage("SKU cannot exceed 100 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-character ISO code.");
    }
}
