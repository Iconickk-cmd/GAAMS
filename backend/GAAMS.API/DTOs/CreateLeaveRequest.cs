
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs
{
    public class CreateLeaveRequest
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string LeaveType { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }
}
