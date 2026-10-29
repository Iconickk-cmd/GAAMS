
using GAAMS.API.Validation;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace GAAMS.API.Tests;

public class EmploymentDateTests
{
    private sealed class EmployeeDates
    {
        public DateTime DateOfBirth { get; set; }

        [EmploymentDate]
        public DateTime DateOfEmployment { get; set; }
    }

    [Fact]
    public void EmploymentDate_BeforeDateOfBirth_FailsValidation()
    {
        var employee = new EmployeeDates
        {
            DateOfBirth = new DateTime(1990, 5, 10),
            DateOfEmployment = new DateTime(1985, 1, 15)
        };

        var results = Validate(employee);

        Assert.Contains(results, result =>
            result.ErrorMessage ==
            "Date of employment cannot be earlier than date of birth.");
    }

    [Fact]
    public void EmploymentDate_AfterDateOfBirth_PassesValidation()
    {
        var employee = new EmployeeDates
        {
            DateOfBirth = new DateTime(1990, 5, 10),
            DateOfEmployment = new DateTime(2020, 1, 15)
        };

        var results = Validate(employee);

        Assert.Empty(results);
    }

    [Fact]
    public void EmploymentDate_OnDateOfBirth_PassesValidation()
    {
        var employee = new EmployeeDates
        {
            DateOfBirth = new DateTime(1990, 5, 10),
            DateOfEmployment = new DateTime(1990, 5, 10)
        };

        var results = Validate(employee);

        Assert.Empty(results);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);

        Validator.TryValidateObject(
            instance,
            context,
            results,
            validateAllProperties: true);

        return results;
    }
}
