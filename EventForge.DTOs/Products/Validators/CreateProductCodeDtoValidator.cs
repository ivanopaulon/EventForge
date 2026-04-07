using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateProductCodeDto"/>.
    /// </summary>
    public class CreateProductCodeDtoValidator : AbstractValidator<CreateProductCodeDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateProductCodeDtoValidator"/> class.
        /// </summary>
        public CreateProductCodeDtoValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("The product is required.");

            RuleFor(x => x.CodeType)
                .NotEmpty()
                .WithMessage("The code type is required.")
                .MaximumLength(30)
                .WithMessage("The code type cannot exceed 30 characters.");

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("The code value is required.")
                .MaximumLength(100)
                .WithMessage("The code value cannot exceed 100 characters.");

            RuleFor(x => x.AlternativeDescription)
                .MaximumLength(200)
                .WithMessage("The alternative description cannot exceed 200 characters.");

            RuleFor(x => x.Status)
                .NotNull()
                .WithMessage("The product code status is required.");
        }
    }
}
