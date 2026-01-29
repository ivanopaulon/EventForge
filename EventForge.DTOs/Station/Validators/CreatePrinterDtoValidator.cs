using FluentValidation;

namespace EventForge.DTOs.Station.Validators
{
    /// <summary>
    /// Validator for <see cref="CreatePrinterDto"/>.
    /// </summary>
    public class CreatePrinterDtoValidator : AbstractValidator<CreatePrinterDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePrinterDtoValidator"/> class.
        /// </summary>
        public CreatePrinterDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The printer name is required.")
                .MaximumLength(50)
                .WithMessage("The printer name cannot exceed 50 characters.");

            RuleFor(x => x.Type)
                .NotEmpty()
                .WithMessage("The printer type is required.")
                .MaximumLength(30)
                .WithMessage("The printer type cannot exceed 30 characters.");

            RuleFor(x => x.Model)
                .MaximumLength(50)
                .WithMessage("The model cannot exceed 50 characters.")
                .When(x => x.Model != null);

            RuleFor(x => x.Location)
                .MaximumLength(50)
                .WithMessage("The location cannot exceed 50 characters.")
                .When(x => x.Location != null);

            RuleFor(x => x.Address)
                .MaximumLength(100)
                .WithMessage("The address cannot exceed 100 characters.")
                .When(x => x.Address != null);

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid printer status.");
        }
    }
}
