using FluentValidation;

namespace EventForge.DTOs.Store.Validators
{
    /// <summary>
    /// Validator for CreateStorePosDto.
    /// </summary>
    public class CreateStorePosDtoValidator : AbstractValidator<CreateStorePosDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStorePosDtoValidator"/> class.
        /// </summary>
        public CreateStorePosDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The POS name is required.")
                .MaximumLength(50)
                .WithMessage("The name cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("The description cannot exceed 200 characters.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value.");

            RuleFor(x => x.Location)
                .MaximumLength(100)
                .WithMessage("The location cannot exceed 100 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("The notes cannot exceed 200 characters.");

            RuleFor(x => x.TerminalIdentifier)
                .MaximumLength(100)
                .WithMessage("The terminal identifier cannot exceed 100 characters.");

            RuleFor(x => x.IPAddress)
                .MaximumLength(45)
                .WithMessage("The IP address cannot exceed 45 characters.");

            RuleFor(x => x.LocationLatitude)
                .InclusiveBetween(-90, 90)
                .When(x => x.LocationLatitude.HasValue)
                .WithMessage("Latitude must be between -90 and 90.");

            RuleFor(x => x.LocationLongitude)
                .InclusiveBetween(-180, 180)
                .When(x => x.LocationLongitude.HasValue)
                .WithMessage("Longitude must be between -180 and 180.");

            RuleFor(x => x.CurrencyCode)
                .MaximumLength(3)
                .WithMessage("The currency code cannot exceed 3 characters.")
                .Matches(@"^[A-Z]{3}$")
                .When(x => !string.IsNullOrEmpty(x.CurrencyCode))
                .WithMessage("Invalid currency code. Use ISO 4217 format (e.g., EUR, USD).");

            RuleFor(x => x.TimeZone)
                .MaximumLength(50)
                .WithMessage("The time zone cannot exceed 50 characters.");
        }
    }
}
