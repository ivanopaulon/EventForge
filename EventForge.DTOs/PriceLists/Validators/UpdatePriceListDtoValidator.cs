using FluentValidation;

namespace EventForge.DTOs.PriceLists.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdatePriceListDto"/>.
    /// </summary>
    public class UpdatePriceListDtoValidator : AbstractValidator<UpdatePriceListDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePriceListDtoValidator"/> class.
        /// </summary>
        public UpdatePriceListDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The name is required.")
                .MaximumLength(100)
                .WithMessage("The name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("The description cannot exceed 500 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .WithMessage("The notes cannot exceed 1000 characters.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid price list status.");

            RuleFor(x => x.Priority)
                .InclusiveBetween(0, 100)
                .WithMessage("Priority must be between 0 and 100.");
        }
    }
}
