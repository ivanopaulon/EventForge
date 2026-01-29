using FluentValidation;
using System;

namespace EventForge.DTOs.Documents.Validators
{
    /// <summary>
    /// Validator for <see cref="CreateDocumentSummaryLinkDto"/>.
    /// </summary>
    public class CreateDocumentSummaryLinkDtoValidator : AbstractValidator<CreateDocumentSummaryLinkDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateDocumentSummaryLinkDtoValidator"/> class.
        /// </summary>
        public CreateDocumentSummaryLinkDtoValidator()
        {
            RuleFor(x => x.SummaryDocumentId)
                .NotEmpty()
                .WithMessage("The summary document ID is required.");
        }
    }
}
