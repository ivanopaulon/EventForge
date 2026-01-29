using FluentValidation;

namespace EventForge.DTOs.Warehouse.Validators
{
    /// <summary>
    /// Validator for CreateStorageLocationDto.
    /// </summary>
    public class CreateStorageLocationDtoValidator : AbstractValidator<CreateStorageLocationDto>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStorageLocationDtoValidator"/> class.
        /// </summary>
        public CreateStorageLocationDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage("Location code is required.")
                .MaximumLength(30)
                .WithMessage("Location code cannot exceed 30 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(100)
                .WithMessage("Description cannot exceed 100 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.WarehouseId)
                .NotEmpty()
                .WithMessage("Warehouse is required.");

            RuleFor(x => x.Capacity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Capacity must be non-negative.")
                .When(x => x.Capacity.HasValue);

            RuleFor(x => x.Occupancy)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Occupancy must be non-negative.")
                .When(x => x.Occupancy.HasValue);

            RuleFor(x => x.Notes)
                .MaximumLength(200)
                .WithMessage("Notes cannot exceed 200 characters.")
                .When(x => x.Notes != null);

            RuleFor(x => x.Zone)
                .MaximumLength(20)
                .WithMessage("Zone cannot exceed 20 characters.")
                .When(x => x.Zone != null);

            RuleFor(x => x.Floor)
                .MaximumLength(10)
                .WithMessage("Floor cannot exceed 10 characters.")
                .When(x => x.Floor != null);

            RuleFor(x => x.Row)
                .MaximumLength(10)
                .WithMessage("Row cannot exceed 10 characters.")
                .When(x => x.Row != null);

            RuleFor(x => x.Column)
                .MaximumLength(10)
                .WithMessage("Column cannot exceed 10 characters.")
                .When(x => x.Column != null);

            RuleFor(x => x.Level)
                .MaximumLength(10)
                .WithMessage("Level cannot exceed 10 characters.")
                .When(x => x.Level != null);
        }
    }
}
