namespace GAAMS.API.DTOs;

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }
}