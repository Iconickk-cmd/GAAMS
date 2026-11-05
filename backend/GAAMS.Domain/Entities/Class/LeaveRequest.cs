
namespace GAAMS.Domain.Entities
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        // Employee requesting leave
        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        // Type of leave: Annual, Sick, Compassionate, Maternity, Paternity, etc.
        public string LeaveType { get; set; } = string.Empty;

        // Leave period
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int TotalDays { get; set; }

        // Reason provided by the employee
        public string Reason { get; set; } = string.Empty;

        // Pending, Approved, Rejected, or Cancelled
        public string Status { get; set; } = "Pending";

        // Approval information
        public int? ReviewedByEmployeeId { get; set; }

        public Employee? ReviewedByEmployee { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewComments { get; set; }

        // Record timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
