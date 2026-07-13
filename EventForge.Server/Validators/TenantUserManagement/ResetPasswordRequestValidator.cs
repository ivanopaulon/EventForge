using EventForge.Server.Controllers;
using FluentValidation;

namespace EventForge.Server.Validators.TenantUserManagement;

/// <summary>
/// FluentValidation validator for ResetPasswordRequest.
/// </summary>
public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Il campo NewPassword è obbligatorio.");
    }
}
