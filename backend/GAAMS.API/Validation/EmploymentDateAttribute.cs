
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.Validation;

[AttributeUsage(AttributeTargets.Property)]
public class EmploymentDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is not DateTime employmentDate)
        {
            return ValidationResult.Success;
        }

        var dateOfBirthProperty = validationContext.ObjectType
            .GetProperty("DateOfBirth");

        if (dateOfBirthProperty is null)
        {
            return ValidationResult.Success;
        }

        var dateOfBirthValue = dateOfBirthProperty
            .GetValue(validationContext.ObjectInstance);

        if (dateOfBirthValue is not DateTime dateOfBirth)
        {
            return ValidationResult.Success;
        }

        if (employmentDate.Date < dateOfBirth.Date)
        {
            return new ValidationResult(
                ErrorMessage ??
                "Date of employment cannot be earlier than date of birth.");
        }

        return ValidationResult.Success;
    }
}
