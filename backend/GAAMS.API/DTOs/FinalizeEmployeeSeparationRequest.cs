
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs;

public class FinalizeEmployeeSeparationRequest
{
    [Required]
    [StringLength(150)]
    public string FinalizedBy { get; set; } = string.Empty;
}
