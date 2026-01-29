using EventForge.DTOs.Common;
using FluentValidation;
using System;

namespace EventForge.DTOs.Documents.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateDocumentRowDto"/>.
    /// </summary>
    public class CreateDocumentRowDtoValidator : AbstractValidator<CreateDocumentRowDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDocumentRowDtoValidator"/> class.
        /// </summary>
        public CreateDocumentRowDtoValidator()
        {
            RuleFor(x => x.DocumentHeaderId)
                .NotEmpty()
                .WithMessage("The document header ID is required.");

            RuleFor(x => x.ProductCode)
                .MaximumLength(50)
                .WithMessage("Product code cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required.")
                .MaximumLength(200)
                .WithMessage("Description cannot exceed 200 characters.");

            RuleFor(x => x.UnitOfMeasure)
                .MaximumLength(10)
                .WithMessage("Unit of measure cannot exceed 10 characters.");

            RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Unit price must be non-negative.");

            RuleFor(x => x.Quantity)
                .InclusiveBetween((decimal)ValidationConstants.MinimumQuantity, (decimal)ValidationConstants.MaximumQuantity)
                .WithMessage("Quantity must be between 0.0001 and 10000.");

            RuleFor(x => x.LineDiscount)
                .InclusiveBetween(0, 100)
                .WithMessage("Line discount must be between 0 and 100.");

            RuleFor(x => x.LineDiscountValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Line discount value must be non-negative.");

            RuleFor(x => x.VatRate)
                .InclusiveBetween(0, 100)
                .WithMessage("VAT rate must be between 0 and 100.");

            RuleFor(x => x.VatDescription)
                .MaximumLength(30)
                .WithMessage("VAT description cannot exceed 30 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("Notes cannot exceed 200 characters.");
        }
    }
}
