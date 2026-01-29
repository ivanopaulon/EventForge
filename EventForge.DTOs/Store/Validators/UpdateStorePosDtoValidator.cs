using FluentValidation;

namespace EventForge.DTOs.Store.Validators
{
    /// <summary>
    /// Validator for UpdateStorePosDto.
    /// </summary>
    public class UpdateStorePosDtoValidator : AbstractValidator<UpdateStorePosDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStorePosDtoValidator"/> class.
        /// </summary>
        public UpdateStorePosDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The POS name is required.")
                .MaximumLength(50)
                .WithMessage("The name cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("The description cannot exceed 200 characters.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value.");

            RuleFor(x => x.Location)
                .MaximumLength(100)
                .WithMessage("The location cannot exceed 100 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("The notes cannot exceed 200 characters.");

            RuleFor(x => x.TerminalIdentifier)
                .MaximumLength(100)
                .WithMessage("The terminal identifier cannot exceed 100 characters.");

            RuleFor(x => x.IPAddress)
                .MaximumLength(45)
                .WithMessage("The IP address cannot exceed 45 characters.");
        }
    }
}
