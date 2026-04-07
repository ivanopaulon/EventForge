using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for UpdateSerialDto.
    /// </summary>
    public class UpdateSerialDtoValidator : AbstractValidator<UpdateSerialDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSerialDtoValidator"/> class.
        /// </summary>
        public UpdateSerialDtoValidator()
        {
            RuleFor(x => x.SerialNumber)
                .MaximumLength(100)
                .WithMessage("Serial number cannot exceed 100 characters.")
                .When(x => x.SerialNumber != null);

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
