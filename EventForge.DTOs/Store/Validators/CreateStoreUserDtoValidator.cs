using FluentValidation;

namespace EventForge.DTOs.Store.Validators
{
    /// <summary>
    /// Validator for CreateStoreUserDto.
    /// </summary>
    public class CreateStoreUserDtoValidator : AbstractValidator<CreateStoreUserDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStoreUserDtoValidator"/> class.
        /// </summary>
        public CreateStoreUserDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The operator name is required.")
                .MaximumLength(100)
                .WithMessage("The name cannot exceed 100 characters.");

            RuleFor(x => x.Username)
                .NotEmpty()
                .WithMessage("The username is required.")
                .MaximumLength(50)
                .WithMessage("The username cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .MaximumLength(100)
                .WithMessage("The email cannot exceed 100 characters.")
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Invalid email address.");

            RuleFor(x => x.PasswordHash)
                .MaximumLength(200)
                .WithMessage("The password hash cannot exceed 200 characters.");

            RuleFor(x => x.Role)
                .MaximumLength(50)
                .WithMessage("The role cannot exceed 50 characters.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value.");

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("The notes cannot exceed 200 characters.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20)
                .WithMessage("The phone number cannot exceed 20 characters.");
        }
    }
}
