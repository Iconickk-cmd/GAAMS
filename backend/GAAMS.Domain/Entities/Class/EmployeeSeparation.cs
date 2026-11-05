
namespace GAAMS.Domain.Entities;

public class EmployeeSeparation
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    // Resignation, Retirement, Dismissal, Redundancy, etc.
    public string SeparationType { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTime ProposedLastWorkingDate { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public string RequestedBy { get; set; } = string.Empty;

    // PendingApproval, Approved, Rejected, PendingClearance, Finalized
    public string Status { get; set; } = "PendingApproval";

    // Management approval information
    public string? DecisionBy { get; set; }
    public DateTime? DecisionAt { get; set; }
    public string? DecisionComments { get; set; }

    // Final separation information
    public string? FinalizedBy { get; set; }
    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmployeeSeparationClearance> Clearances
    { get; set; } = new List<EmployeeSeparationClearance>();
}
