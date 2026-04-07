using FluentValidation;

namespace EventForge.DTOs.Store.Validators
{
    /// <summary>
    /// Validator for UpdateStoreUserGroupDto.
    /// </summary>
    public class UpdateStoreUserGroupDtoValidator : AbstractValidator<UpdateStoreUserGroupDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStoreUserGroupDtoValidator"/> class.
        /// </summary>
        public UpdateStoreUserGroupDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("The group code is required.")
                .MaximumLength(50)
                .WithMessage("The code cannot exceed 50 characters.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The group name is required.")
                .MaximumLength(50)
                .WithMessage("The name cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("The description cannot exceed 200 characters.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value.");

            RuleFor(x => x.ColorHex)
                .MaximumLength(7)
                .WithMessage("The color hex cannot exceed 7 characters.")
                .Matches(@"^#([A-Fa-f0-9]{6})$")
                .When(x => !string.IsNullOrEmpty(x.ColorHex))
                .WithMessage("Invalid color format. Use #RRGGBB format.");
        }
    }
}
