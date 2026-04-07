using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateProductSupplierDto"/>.
    /// </summary>
    public class UpdateProductSupplierDtoValidator : AbstractValidator<UpdateProductSupplierDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateProductSupplierDtoValidator"/> class.
        /// </summary>
        public UpdateProductSupplierDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("The product is required.");

            RuleFor(x => x.SupplierId)
                .NotEmpty()
                .WithMessage("The supplier is required.");

            RuleFor(x => x.SupplierProductCode)
                .MaximumLength(100)
                .WithMessage("The supplier product code cannot exceed 100 characters.");

            RuleFor(x => x.PurchaseDescription)
                .MaximumLength(500)
                .WithMessage("The purchase description cannot exceed 500 characters.");

            RuleFor(x => x.Currency)
                .MaximumLength(10)
                .WithMessage("The currency cannot exceed 10 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("The notes cannot exceed 1000 characters.");
        }
    }
}
