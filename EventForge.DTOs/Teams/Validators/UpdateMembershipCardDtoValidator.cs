using System;
using FluentValidation;

namespace EventForge.DTOs.Teams.Validators
{
    /// <summary>
    /// Validator for <see cref="UpdateMembershipCardDto"/>.
    /// </summary>
    public class UpdateMembershipCardDtoValidator : AbstractValidator<UpdateMembershipCardDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateMembershipCardDtoValidator"/> class.
        /// </summary>
        public UpdateMembershipCardDtoValidator()
        {
            RuleFor(x => x.CardNumber)
                .MaximumLength(50)
                .WithMessage("The card number cannot exceed 50 characters.")
                .When(x => x.CardNumber != null);

            RuleFor(x => x.Federation)
                .MaximumLength(100)
                .WithMessage("The federation cannot exceed 100 characters.")
                .When(x => x.Federation != null);

            RuleFor(x => x.ValidTo)
                .GreaterThan(x => x.ValidFrom ?? DateTime.MinValue)
                .WithMessage("Valid to date must be after valid from date.")
                .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue);

            RuleFor(x => x.Category)
                .MaximumLength(50)
                .WithMessage("The category cannot exceed 50 characters.")
                .When(x => x.Category != null);

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("The notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
