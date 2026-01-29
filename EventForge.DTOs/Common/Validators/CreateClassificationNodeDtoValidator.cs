using FluentValidation;

namespace EventForge.DTOs.Common.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateClassificationNodeDto"/>.
    /// </summary>
    public class CreateClassificationNodeDtoValidator : AbstractValidator<CreateClassificationNodeDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateClassificationNodeDtoValidator"/> class.
        /// </summary>
        public CreateClassificationNodeDtoValidator()
        {
            RuleFor(x => x.Code)
                .MaximumLength(30)
                .WithMessage("Code cannot exceed 30 characters.")
                .When(x => x.Code != null);

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

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
