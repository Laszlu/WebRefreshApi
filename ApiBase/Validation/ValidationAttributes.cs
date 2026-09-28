using System.ComponentModel.DataAnnotations;

namespace ApiBase.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NonEmptyGuid : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is null)
            return ValidationResult.Success;

        if (value is Guid guid && guid == Guid.Empty)
            return new ValidationResult(
                ErrorMessage ?? $"{context.MemberName} can not be Guid.Empty (00000000-0000-0000-0000-000000000000)");

        return ValidationResult.Success;
    }
}
