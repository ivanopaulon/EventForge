using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for AdjustStockDto.
    /// </summary>
    public class AdjustStockDtoValidator : AbstractValidator<AdjustStockDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdjustStockDtoValidator"/> class.
        /// </summary>
        public AdjustStockDtoValidator()
        {
            RuleFor(x => x.StockId)
                .NotEmpty()
                .WithMessage("Stock ID is required.");

            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product is required.");

            RuleFor(x => x.StorageLocationId)
                .NotEmpty()
                .WithMessage("Storage location is required.");

            RuleFor(x => x.NewQuantity)
                .NotEmpty()
                .WithMessage("New quantity is required.")
                .InclusiveBetween(0, 999999999)
                .WithMessage("New quantity must be between 0 and 999999999.");

            RuleFor(x => x.PreviousQuantity)
                .NotEmpty()
                .WithMessage("Previous quantity is required.")
                .InclusiveBetween(0, 999999999)
                .WithMessage("Previous quantity must be between 0 and 999999999.");

            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Reason is required.");
        }
    }
}
