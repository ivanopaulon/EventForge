using FluentValidation;

namespace EventForge.DTOs.Banks.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateBankDto"/>.
    /// </summary>
    public class CreateBankDtoValidator : AbstractValidator<CreateBankDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateBankDtoValidator"/> class.
        /// </summary>
        public CreateBankDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The bank name is required.")
                .MaximumLength(100)
                .WithMessage("The bank name cannot exceed 100 characters.");

            RuleFor(x => x.Code)
                .MaximumLength(20)
                .WithMessage("The bank code cannot exceed 20 characters.")
                .When(x => x.Code != null);

            RuleFor(x => x.SwiftBic)
                .MaximumLength(20)
                .WithMessage("The SWIFT/BIC code cannot exceed 20 characters.")
                .When(x => x.SwiftBic != null);

            RuleFor(x => x.Branch)
                .MaximumLength(100)
                .WithMessage("The branch name cannot exceed 100 characters.")
                .When(x => x.Branch != null);

            RuleFor(x => x.Address)
                .MaximumLength(200)
                .WithMessage("The address cannot exceed 200 characters.")
                .When(x => x.Address != null);

            RuleFor(x => x.Country)
                .MaximumLength(50)
                .WithMessage("The country name cannot exceed 50 characters.")
                .When(x => x.Country != null);

            RuleFor(x => x.Phone)
                .MaximumLength(30)
                .WithMessage("The phone number cannot exceed 30 characters.")
                .When(x => x.Phone != null);

            RuleFor(x => x.Email)
                .MaximumLength(100)
                .WithMessage("The email cannot exceed 100 characters.")
                .EmailAddress()
                .WithMessage("Invalid email address format.")
                .When(x => x.Email != null);

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("The notes cannot exceed 200 characters.")
                .When(x => x.Notes != null);
        }
    }
}
