using EventForge.Server.Controllers.Api;
using FluentValidation;

namespace EventForge.Server.Validators.ServerAuth;

/// <summary>
/// FluentValidation validator for ServerLoginRequestDto.
/// </summary>
public class ServerLoginRequestDtoValidator : AbstractValidator<ServerLoginRequestDto>
{
    public ServerLoginRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Il campo Username è obbligatorio.")
            .MaximumLength(100)
            .WithMessage("Il campo Username non può superare 100 caratteri.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Il campo Password è obbligatorio.");
    }
}
