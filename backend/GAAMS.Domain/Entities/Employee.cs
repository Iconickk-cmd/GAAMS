namespace GAAMS.Domain.Entities;

public class Employee
{
    public int Id { get; set; }

    public string EmployeeNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? NationalId { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime DateOfBirth { get; set; }

    public DateTime DateOfEmployment { get; set; }

    public string? JobTitle { get; set; }

    public int DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Department? Department { get; set; }
}