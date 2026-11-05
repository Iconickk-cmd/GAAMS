
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs
{
    public class ReviewLeaveRequest
    {
        [Required]
        public int ReviewedByEmployeeId { get; set; }

        [Required]
        [RegularExpression("^(Approved|Rejected)$",
            ErrorMessage = "Status must be Approved or Rejected.")]
        public string Status { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? ReviewComments { get; set; }
    }
}
