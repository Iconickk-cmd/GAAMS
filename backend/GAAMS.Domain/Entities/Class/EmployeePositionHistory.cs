
namespace GAAMS.Domain.Entities;

public class EmployeePositionHistory
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public int PositionId { get; set; }

    public Position? Position { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? ChangeReason { get; set; }

    public bool IsCurrent { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
