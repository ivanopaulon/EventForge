using FluentValidation;

namespace EventForge.DTOs.Tenants.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateTenantUserDto"/>.
    /// </summary>
    public class CreateTenantUserDtoValidator : AbstractValidator<CreateTenantUserDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTenantUserDtoValidator"/> class.
        /// </summary>
        public CreateTenantUserDtoValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("Username is required.")
                .MaximumLength(100)
                .WithMessage("Username cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .MaximumLength(256)
                .WithMessage("Email cannot exceed 256 characters.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required.")
                .MaximumLength(100)
                .WithMessage("First name cannot exceed 100 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required.")
                .MaximumLength(100)
                .WithMessage("Last name cannot exceed 100 characters.");
        }
    }
}
