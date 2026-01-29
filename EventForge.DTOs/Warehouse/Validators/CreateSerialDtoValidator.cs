using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for CreateSerialDto.
    /// </summary>
    public class CreateSerialDtoValidator : AbstractValidator<CreateSerialDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateSerialDtoValidator"/> class.
        /// </summary>
        public CreateSerialDtoValidator()
        {
            RuleFor(x => x.SerialNumber)
                .NotEmpty()
                .WithMessage("Serial number is required.")
                .MaximumLength(100)
                .WithMessage("Serial number cannot exceed 100 characters.");

            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product is required.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);

            RuleFor(x => x.Barcode)
                .MaximumLength(50)
                .WithMessage("Barcode cannot exceed 50 characters.")
                .When(x => x.Barcode != null);

            RuleFor(x => x.RfidTag)
                .MaximumLength(50)
                .WithMessage("RFID tag cannot exceed 50 characters.")
                .When(x => x.RfidTag != null);
        }
    }
}
