using EventForge.DTOs.Auth;
using FluentValidation;

namespace EventForge.Server.Validators.Auth;

/// <summary>
/// FluentValidation validator for LoginRequestDto.
/// </summary>
public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.TenantCode)
            .NotEmpty()
            .WithMessage("Tenant code is required.")
            .MaximumLength(50)
            .WithMessage("Tenant code cannot exceed 50 characters.");

        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(100)
            .WithMessage("Username cannot exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
