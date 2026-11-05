
namespace GAAMS.Domain.Entities;

public class EmployeeSeparationClearanceAudit
{
    public int Id { get; set; }

    public int EmployeeSeparationClearanceId { get; set; }

    public string PreviousClearedBy { get; set; } = string.Empty;

    public string? PreviousRemarks { get; set; }

    public DateTime? PreviousClearedAt { get; set; }

    public string CorrectedClearedBy { get; set; } = string.Empty;

    public string? CorrectedRemarks { get; set; }

    public string CorrectionReason { get; set; } = string.Empty;

    public string CorrectedBy { get; set; } = string.Empty;

    public DateTime CorrectedAt { get; set; } = DateTime.UtcNow;
}
