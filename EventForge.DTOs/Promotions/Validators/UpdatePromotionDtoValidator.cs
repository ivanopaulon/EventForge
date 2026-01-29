using FluentValidation;

namespace EventForge.DTOs.Promotions.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdatePromotionDto"/>.
    /// </summary>
    public class UpdatePromotionDtoValidator : AbstractValidator<UpdatePromotionDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatePromotionDtoValidator"/> class.
        /// </summary>
        public UpdatePromotionDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required.")
                .MaximumLength(100)
                .WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("End date must be greater than or equal to start date.");

            RuleFor(x => x.CouponCode)
                .MaximumLength(50)
                .WithMessage("Coupon code cannot exceed 50 characters.")
                .When(x => x.CouponCode != null);
        }
    }
}
