
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs;

public class CreateEmployeeSeparationRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(50)]
    public string SeparationType { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    public DateTime ProposedLastWorkingDate { get; set; }

    [Required]
    [StringLength(150)]
    public string RequestedBy { get; set; } = string.Empty;
}
