using System.ComponentModel.DataAnnotations;
namespace GAAMS.API.DTOs;

public class CreateEmployeeRequest
{
    [GAAMS.API.Validation.NoSurroundingWhitespace]
    [Required]
    [StringLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [GAAMS.API.Validation.NoSurroundingWhitespace]
    [StringLength(20)]
    public string? NationalId { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    [GAAMS.API.Validation.NotInFuture]
    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [GAAMS.API.Validation.NotInFuture]
    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfEmployment { get; set; }

    public string? JobTitle { get; set; }

    public int DepartmentId { get; set; }
}