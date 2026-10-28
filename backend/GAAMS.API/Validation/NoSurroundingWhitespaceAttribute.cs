
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.Validation;

public class NoSurroundingWhitespaceAttribute : ValidationAttribute
{
    public NoSurroundingWhitespaceAttribute()
    {
        ErrorMessage = "This field cannot start or end with spaces.";
    }

    public override bool IsValid(object? value)
    {
        if (value is not string text)
        {
            return true;
        }

        return text == text.Trim();
    }
}
