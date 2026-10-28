
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.Validation;

public class NotInFutureAttribute : ValidationAttribute
{
    public NotInFutureAttribute()
    {
        ErrorMessage = "This date cannot be in the future.";
    }
    public override bool IsValid(object? value)
    {
        if (value is not DateTime date)
        {
            return true;
        }

        return date.Date <= DateTime.UtcNow.Date;
    }
}
