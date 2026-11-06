
namespace GAAMS.Domain.Entities
{
    public class LeaveBalance
    {
        public int Id { get; set; }

        // Employee whose leave balance is being tracked
        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        // Annual, Sick, Compassionate, etc.
        public string LeaveType { get; set; } = "Annual";

        // Calendar year: January 1 to December 31
        public int LeaveYear { get; set; }

        // Total allocated leave days
        public decimal EntitledDays { get; set; }

        // Leave days earned during the year
        public decimal AccruedDays { get; set; }

        // Approved leave already taken
        public decimal TakenDays { get; set; }

        // Leave requests awaiting approval
        public decimal PendingDays { get; set; }

        // Available leave days
        public decimal RemainingDays { get; set; }

        // Record timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
