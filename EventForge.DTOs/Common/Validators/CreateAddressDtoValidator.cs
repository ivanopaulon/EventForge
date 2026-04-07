using FluentValidation;

namespace EventForge.DTOs.Common.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateAddressDto"/>.
    /// </summary>
    public class CreateAddressDtoValidator : AbstractValidator<CreateAddressDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAddressDtoValidator"/> class.
        /// </summary>
        public CreateAddressDtoValidator()
        {
            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Owner ID is required.");

            RuleFor(x => x.OwnerType)
                .NotEmpty()
                .WithMessage("Owner type is required.")
                .MaximumLength(50)
                .WithMessage("Owner type cannot exceed 50 characters.");

            RuleFor(x => x.Street)
                .MaximumLength(100)
                .WithMessage("Street cannot exceed 100 characters.")
                .When(x => x.Street != null);

            RuleFor(x => x.City)
                .MaximumLength(50)
                .WithMessage("City cannot exceed 50 characters.")
                .When(x => x.City != null);

            RuleFor(x => x.ZipCode)
                .MaximumLength(10)
                .WithMessage("ZIP code cannot exceed 10 characters.")
                .When(x => x.ZipCode != null);

            RuleFor(x => x.Province)
                .MaximumLength(50)
                .WithMessage("Province cannot exceed 50 characters.")
                .When(x => x.Province != null);

            RuleFor(x => x.Country)
                .MaximumLength(50)
                .WithMessage("Country cannot exceed 50 characters.")
                .When(x => x.Country != null);

            RuleFor(x => x.Notes)
                .MaximumLength(100)
                .WithMessage("Notes cannot exceed 100 characters.")
                .When(x => x.Notes != null);
        }
    }
}
