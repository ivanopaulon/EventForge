using FluentValidation;

namespace EventForge.DTOs.Sales.Validators
{
    /// <summary>
    /// Validator for CreateSaleSessionDto.
    /// </summary>
    public class CreateSaleSessionDtoValidator : AbstractValidator<CreateSaleSessionDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSaleSessionDtoValidator"/> class.
        /// </summary>
        public CreateSaleSessionDtoValidator()
        {
            RuleFor(x => x.OperatorId)
                .NotEmpty()
                .WithMessage("Operator ID is required");

            RuleFor(x => x.PosId)
                .NotEmpty()
                .WithMessage("POS ID is required");

            RuleFor(x => x.SaleType)
                .MaximumLength(50)
                .WithMessage("Sale type cannot exceed 50 characters.")
                .When(x => x.SaleType != null);

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Currency is required.")
                .MaximumLength(3)
                .WithMessage("Currency code must not exceed 3 characters.");
        }
    }
}
