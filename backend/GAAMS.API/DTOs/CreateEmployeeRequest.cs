
using System.ComponentModel.DataAnnotations;
using GAAMS.API.Validation;

namespace GAAMS.API.DTOs;

public class CreateEmployeeRequest
{
    [NoSurroundingWhitespace]
    [Required]
    [StringLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [NoSurroundingWhitespace]
    [StringLength(20)]
    public string? NationalId { get; set; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; set; }

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [NotInFuture]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [NotInFuture]
    [EmploymentDate]
    public DateTime DateOfEmployment { get; set; }

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }
}
