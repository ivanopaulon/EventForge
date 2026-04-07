using FluentValidation;

namespace EventForge.DTOs.UnitOfMeasures.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateUMDto"/>.
    /// </summary>
    public class CreateUMDtoValidator : AbstractValidator<CreateUMDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateUMDtoValidator"/> class.
        /// </summary>
        public CreateUMDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The name is required.")
                .MaximumLength(50)
                .WithMessage("The name cannot exceed 50 characters.");

            RuleFor(x => x.Symbol)
                .NotEmpty()
                .WithMessage("The symbol is required.")
                .MaximumLength(10)
                .WithMessage("The symbol cannot exceed 10 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("The description cannot exceed 200 characters.")
                .When(x => x.Description != null);
        }
    }
}
