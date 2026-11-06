
namespace GAAMS.Domain.Entities
{
    public class LeaveEntitlement
    {
        public int Id { get; set; }

        // Employee receiving the leave entitlement
        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        // Type of leave: Annual, Sick, Compassionate, etc.
        public string LeaveType { get; set; } = "Annual";

        // Number of leave days allocated
        public decimal EntitlementDays { get; set; } = 21m;

        // Calendar year to which this entitlement applies
        public int LeaveYear { get; set; }

        // Entitlement validity period
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        // Whether this entitlement is currently active
        public bool IsActive { get; set; } = true;

        // Audit timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
