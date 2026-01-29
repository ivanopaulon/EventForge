using FluentValidation;

namespace EventForge.DTOs.Store.Validators
{
    /// <summary>
    /// Validator for UpdateStoreUserPrivilegeDto.
    /// </summary>
    public class UpdateStoreUserPrivilegeDtoValidator : AbstractValidator<UpdateStoreUserPrivilegeDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateStoreUserPrivilegeDtoValidator"/> class.
        /// </summary>
        public UpdateStoreUserPrivilegeDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("The privilege code is required.")
                .MaximumLength(50)
                .WithMessage("The code cannot exceed 50 characters.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("The privilege name is required.")
                .MaximumLength(50)
                .WithMessage("The name cannot exceed 50 characters.");

            RuleFor(x => x.Category)
                .MaximumLength(50)
                .WithMessage("The category cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(200)
                .WithMessage("The description cannot exceed 200 characters.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Invalid status value.");

            RuleFor(x => x.Resource)
                .MaximumLength(100)
                .WithMessage("The resource cannot exceed 100 characters.");

            RuleFor(x => x.Action)
                .MaximumLength(50)
                .WithMessage("The action cannot exceed 50 characters.");

            RuleFor(x => x.PermissionKey)
                .MaximumLength(200)
                .WithMessage("The permission key cannot exceed 200 characters.");
        }
    }
}
