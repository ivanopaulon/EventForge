using FluentValidation;

namespace EventForge.DTOs.Products.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateModelDto"/>.
    /// </summary>
    public class UpdateModelDtoValidator : AbstractValidator<UpdateModelDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateModelDtoValidator"/> class.
        /// </summary>
        public UpdateModelDtoValidator()
        {
            RuleFor(x => x.BrandId)
                .NotEmpty()
                .WithMessage("The brand is required.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The model name is required.")
                .MaximumLength(200)
                .WithMessage("The model name cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000)
                .WithMessage("The description cannot exceed 1000 characters.");

            RuleFor(x => x.ManufacturerPartNumber)
                .MaximumLength(100)
                .WithMessage("The manufacturer part number cannot exceed 100 characters.");
        }
    }
}
