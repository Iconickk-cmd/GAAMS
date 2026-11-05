
using System;
using System.Text.Json.Serialization;

namespace GAAMS.Domain.Entities;

public class EmployeeSeparationClearance
{
    public int Id { get; set; }

    public int EmployeeSeparationId { get; set; }

    [JsonIgnore]
    public EmployeeSeparation? EmployeeSeparation { get; set; }

    // HR, Finance, IT, Security, etc.
    public string DepartmentName { get; set; } = string.Empty;

    // Pending, Completed, NotRequired
    public string Status { get; set; } = "Pending";

    public string? Remarks { get; set; }

    public string? ClearedBy { get; set; }

    public DateTime? ClearedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
