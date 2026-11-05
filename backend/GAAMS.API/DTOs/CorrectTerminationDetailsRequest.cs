using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs;

public class CorrectTerminationDetailsRequest
{
    [Required]
    [StringLength(500)]
    public string TerminationReason { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string TerminationType { get; set; } = string.Empty;
}