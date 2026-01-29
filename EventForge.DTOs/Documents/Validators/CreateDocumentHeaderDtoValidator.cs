using FluentValidation;
using System;

namespace EventForge.DTOs.Documents.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateDocumentHeaderDto"/>.
    /// </summary>
    public class CreateDocumentHeaderDtoValidator : AbstractValidator<CreateDocumentHeaderDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDocumentHeaderDtoValidator"/> class.
        /// </summary>
        public CreateDocumentHeaderDtoValidator()
        {
            RuleFor(x => x.DocumentTypeId)
                .NotEmpty()
                .WithMessage("Document type is required.");

            RuleFor(x => x.Series)
                .MaximumLength(10)
                .WithMessage("Series cannot exceed 10 characters.");

            RuleFor(x => x.Number)
                .MaximumLength(30)
                .WithMessage("Number cannot exceed 30 characters.");

            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Document date is required.");

            RuleFor(x => x.BusinessPartyId)
                .NotEmpty()
                .WithMessage("Business party is required.");

            RuleFor(x => x.CustomerName)
                .MaximumLength(100)
                .WithMessage("Customer name cannot exceed 100 characters.");

            RuleFor(x => x.CarrierName)
                .MaximumLength(100)
                .WithMessage("Carrier name cannot exceed 100 characters.");

            RuleFor(x => x.TrackingNumber)
                .MaximumLength(50)
                .WithMessage("Tracking number cannot exceed 50 characters.");

            RuleFor(x => x.ShippingNotes)
                .MaximumLength(200)
                .WithMessage("Shipping notes cannot exceed 200 characters.");

            RuleFor(x => x.ExternalDocumentNumber)
                .MaximumLength(30)
                .WithMessage("External document number cannot exceed 30 characters.");

            RuleFor(x => x.ExternalDocumentSeries)
                .MaximumLength(10)
                .WithMessage("External document series cannot exceed 10 characters.");

            RuleFor(x => x.DocumentReason)
                .MaximumLength(100)
                .WithMessage("Reason cannot exceed 100 characters.");

            RuleFor(x => x.FiscalDocumentNumber)
                .MaximumLength(30)
                .WithMessage("Fiscal document number cannot exceed 30 characters.");

            RuleFor(x => x.Currency)
                .MaximumLength(3)
                .WithMessage("Currency code cannot exceed 3 characters.");

            RuleFor(x => x.PaymentMethod)
                .MaximumLength(30)
                .WithMessage("Payment method cannot exceed 30 characters.");

            RuleFor(x => x.PaymentReference)
                .MaximumLength(50)
                .WithMessage("Payment reference cannot exceed 50 characters.");

            RuleFor(x => x.TotalDiscount)
                .InclusiveBetween(0, 100)
                .WithMessage("Total discount must be between 0 and 100.");

            RuleFor(x => x.TotalDiscountAmount)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Total discount amount must be non-negative.");

            RuleFor(x => x.Notes)
                .MaximumLength(500)
                .WithMessage("Notes cannot exceed 500 characters.");
        }
    }
}
