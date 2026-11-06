
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs;

public class CorrectEmployeeSeparationClearanceRequest
{
    [Required]
    [StringLength(150)]
    public string CorrectedClearedBy { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? CorrectedRemarks { get; set; }

    [Required]
    [StringLength(1000)]
    public string CorrectionReason { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string CorrectedBy { get; set; } = string.Empty;
}
