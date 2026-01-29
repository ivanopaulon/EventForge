using FluentValidation;

namespace EventForge.DTOs.Business.Validators
{
    /// <summary>
    /// Validator for CreatePaymentTermDto.
    /// </summary>
    public class CreatePaymentTermDtoValidator : AbstractValidator<CreatePaymentTermDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePaymentTermDtoValidator"/> class.
        /// </summary>
        public CreatePaymentTermDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The name is required.")
                .MaximumLength(100)
                .WithMessage("The name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(250)
                .WithMessage("The description cannot exceed 250 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.DueDays)
                .InclusiveBetween(0, 365)
                .WithMessage("Due days must be between 0 and 365.");

            RuleFor(x => x.PaymentMethod)
                .IsInEnum()
                .WithMessage("A valid payment method is required.");
        }
    }
}
