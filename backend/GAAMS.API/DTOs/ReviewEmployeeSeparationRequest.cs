
using System.ComponentModel.DataAnnotations;

namespace GAAMS.API.DTOs;

public class ReviewEmployeeSeparationRequest
{
    public bool Approved { get; set; }

    [Required]
    [StringLength(150)]
    public string DecisionBy { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? DecisionComments { get; set; }

    // Required when approving a request.
    public List<string> ClearanceDepartments { get; set; } = new();
}
