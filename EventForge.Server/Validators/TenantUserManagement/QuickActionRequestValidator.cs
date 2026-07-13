using EventForge.Server.Controllers;
using FluentValidation;

namespace EventForge.Server.Validators.TenantUserManagement;

/// <summary>
/// FluentValidation validator for QuickActionRequest.
/// </summary>
public class QuickActionRequestValidator : AbstractValidator<QuickActionRequest>
{
    public QuickActionRequestValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .WithMessage("Il campo Action è obbligatorio.");
    }
}
