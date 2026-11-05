
namespace GAAMS.Domain.Entities;

public class Position
{
    public int Id { get; set; }

    public string PositionCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string? EmploymentType { get; set; }

    public string? EmploymentGrade { get; set; }

    public int ApprovedHeadcount { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
