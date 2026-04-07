using FluentValidation;

namespace EventForge.DTOs.Sales.Validators
{
    /// <summary>
    /// Validator for UpdateSaleSessionDto.
    /// </summary>
    public class UpdateSaleSessionDtoValidator : AbstractValidator<UpdateSaleSessionDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSaleSessionDtoValidator"/> class.
        /// </summary>
        public UpdateSaleSessionDtoValidator()
        {
            RuleFor(x => x.SaleType)
                .MaximumLength(50)
                .WithMessage("Sale type cannot exceed 50 characters.")
                .When(x => x.SaleType != null);

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("A valid sale session status is required.")
                .When(x => x.Status.HasValue);
        }
    }
}
