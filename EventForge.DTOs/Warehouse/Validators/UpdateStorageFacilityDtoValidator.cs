using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for UpdateStorageFacilityDto.
    /// </summary>
    public class UpdateStorageFacilityDtoValidator : AbstractValidator<UpdateStorageFacilityDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStorageFacilityDtoValidator"/> class.
        /// </summary>
        public UpdateStorageFacilityDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Address)
                .MaximumLength(200)
                .WithMessage("Address cannot exceed 200 characters.")
                .When(x => x.Address != null);

            RuleFor(x => x.Phone)
                .MaximumLength(30)
                .WithMessage("Phone cannot exceed 30 characters.")
                .When(x => x.Phone != null);

            RuleFor(x => x.Email)
                .MaximumLength(100)
                .WithMessage("Email cannot exceed 100 characters.")
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .When(x => x.Email != null);

            RuleFor(x => x.Manager)
                .MaximumLength(100)
                .WithMessage("Manager cannot exceed 100 characters.")
                .When(x => x.Manager != null);

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);

            RuleFor(x => x.AreaSquareMeters)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Area must be non-negative.")
                .When(x => x.AreaSquareMeters.HasValue);

            RuleFor(x => x.Capacity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Capacity must be non-negative.")
                .When(x => x.Capacity.HasValue);
        }
    }
}
