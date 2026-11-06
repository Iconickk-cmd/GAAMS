
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs
{
    public class CreateEmployeeRequest
    {
        [Required]
        public string EmployeeNumber { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? NationalId { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public DateTime DateOfEmployment { get; set; }

        public string? JobTitle { get; set; }

        [Range(1, int.MaxValue)]
        public int DepartmentId { get; set; }
    }
}
