using FluentValidation;

namespace EventForge.DTOs.Common.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateClassificationNodeDto"/>.
    /// </summary>
    public class UpdateClassificationNodeDtoValidator : AbstractValidator<UpdateClassificationNodeDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateClassificationNodeDtoValidator"/> class.
        /// </summary>
        public UpdateClassificationNodeDtoValidator()
        {
            RuleFor(x => x.Code)
                .MaximumLength(30)
                .WithMessage("Code cannot exceed 30 characters.")
                .When(x => x.Code != null);

            RuleFor(x => x.Name)
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.")
                .When(x => x.Name != null);

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("Description cannot exceed 200 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("Type is required.");

            RuleFor(x => x.Status)
                .NotEmpty()
                .WithMessage("Status is required.");

            RuleFor(x => x.Level)
                .InclusiveBetween(0, 10)
                .WithMessage("Level must be between 0 and 10.");

            RuleFor(x => x.Order)
                .InclusiveBetween(0, 1000)
                .WithMessage("Order must be between 0 and 1000.");
        }
    }
}
