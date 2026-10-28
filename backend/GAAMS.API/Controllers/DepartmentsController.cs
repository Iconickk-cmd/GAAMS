using GAAMS.API.DTOs;
using GAAMS.Domain.Entities;
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DepartmentsController : ControllerBase
{
    private readonly GAAMSDbContext _context;

    public DepartmentsController(GAAMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
    {
        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(departments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Department>> GetDepartment(int id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound();
        }

        return Ok(department);
    }

    [HttpPost]
    public async Task<ActionResult<Department>> CreateDepartment(
        CreateDepartmentRequest request)
    {
        var department = new Department
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description
        };

        _context.Departments.Add(department);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDepartments),
            new { id = department.Id },
            department);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDepartment(
    int id,
    UpdateDepartmentRequest request)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound();
        }

        department.Name = request.Name;
        department.Code = request.Code;
        department.Description = request.Description;
        department.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(department);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound();
        }

        var hasEmployees = await _context.Employees
            .AnyAsync(e => e.DepartmentId == id);

        if (hasEmployees)
        {
            return Conflict(new
            {
                message = "This department cannot be deleted because it has employees assigned to it."
            });
        }

        _context.Departments.Remove(department);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}