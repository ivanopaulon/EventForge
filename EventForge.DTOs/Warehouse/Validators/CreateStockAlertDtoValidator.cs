using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for CreateStockAlertDto.
    /// </summary>
    public class CreateStockAlertDtoValidator : AbstractValidator<CreateStockAlertDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStockAlertDtoValidator"/> class.
        /// </summary>
        public CreateStockAlertDtoValidator()
        {
            RuleFor(x => x.StockId)
                .NotEmpty()
                .WithMessage("Stock entry is required.");

            RuleFor(x => x.AlertType)
                .NotEmpty()
                .WithMessage("Alert type is required.");

            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Message is required.")
                .MaximumLength(500)
                .WithMessage("Message cannot exceed 500 characters.");

            RuleFor(x => x.NotificationEmails)
                .MaximumLength(500)
                .WithMessage("Notification emails cannot exceed 500 characters.")
                .When(x => x.NotificationEmails != null);
        }
    }
}
