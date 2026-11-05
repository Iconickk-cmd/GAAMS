
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs;

public class CompleteEmployeeSeparationClearanceRequest
{
    [Required]
    [StringLength(150)]
    public string ClearedBy { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Remarks { get; set; }

    public bool IsCleared { get; set; }
}
