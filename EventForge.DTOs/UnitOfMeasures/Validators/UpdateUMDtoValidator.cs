using FluentValidation;

namespace EventForge.DTOs.UnitOfMeasures.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateUMDto"/>.
    /// </summary>
    public class UpdateUMDtoValidator : AbstractValidator<UpdateUMDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateUMDtoValidator"/> class.
        /// </summary>
        public UpdateUMDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The name is required.")
                .MaximumLength(50)
                .WithMessage("The name cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("The description cannot exceed 200 characters.")
                .When(x => x.Description != null);
        }
    }
}
