using FluentValidation;

namespace EventForge.DTOs.Business.Validators
{
    /// <summary>
    /// Validator for CreateBusinessPartyDto.
    /// </summary>
    public class CreateBusinessPartyDtoValidator : AbstractValidator<CreateBusinessPartyDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateBusinessPartyDtoValidator"/> class.
        /// </summary>
        public CreateBusinessPartyDtoValidator()
        {
            RuleFor(x => x.PartyType)
                .IsInEnum()
                .WithMessage("A valid party type is required.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The name is required.")
                .MaximumLength(200)
                .WithMessage("The name cannot exceed 200 characters.");

            RuleFor(x => x.TaxCode)
                .MaximumLength(20)
                .WithMessage("The tax code cannot exceed 20 characters.")
                .When(x => x.TaxCode != null);

            RuleFor(x => x.VatNumber)
                .MaximumLength(20)
                .WithMessage("The VAT number cannot exceed 20 characters.")
                .When(x => x.VatNumber != null);

            RuleFor(x => x.SdiCode)
                .MaximumLength(10)
                .WithMessage("The SDI code cannot exceed 10 characters.")
                .When(x => x.SdiCode != null);

            RuleFor(x => x.Pec)
                .MaximumLength(100)
                .WithMessage("The PEC cannot exceed 100 characters.")
                .When(x => x.Pec != null);

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("The notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
