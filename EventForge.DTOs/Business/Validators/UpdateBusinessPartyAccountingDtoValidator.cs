using FluentValidation;

namespace EventForge.DTOs.Business.Validators
{
    /// <summary>
    /// Validator for UpdateBusinessPartyAccountingDto.
    /// </summary>
    public class UpdateBusinessPartyAccountingDtoValidator : AbstractValidator<UpdateBusinessPartyAccountingDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateBusinessPartyAccountingDtoValidator"/> class.
        /// </summary>
        public UpdateBusinessPartyAccountingDtoValidator()
        {
            RuleFor(x => x.BusinessPartyId)
                .NotEmpty()
                .WithMessage("The business party ID is required.");

            RuleFor(x => x.Iban)
                .MaximumLength(34)
                .WithMessage("The IBAN cannot exceed 34 characters.")
                .When(x => x.Iban != null);

            RuleFor(x => x.CreditLimit)
                .GreaterThanOrEqualTo(0)
                .WithMessage("The credit limit must be a positive value.")
                .When(x => x.CreditLimit.HasValue);

            RuleFor(x => x.Notes)
                .MaximumLength(100)
                .WithMessage("The notes cannot exceed 100 characters.")
                .When(x => x.Notes != null);
        }
    }
}
