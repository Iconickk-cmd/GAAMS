
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

    // GET: api/departments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(departments);
    }

    // GET: api/departments/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Department>> GetDepartment(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
            return NotFound(new { message = "Department not found." });

        return Ok(department);
    }

    // POST: api/departments
    [HttpPost]
    public async Task<ActionResult<Department>> CreateDepartment(
        CreateDepartmentRequest request)
    {
        var nameExists = await _context.Departments
            .AnyAsync(d => d.Name.ToLower() == request.Name.Trim().ToLower());

        if (nameExists)
            return Conflict(new { message = "A department with this name already exists." });

        var codeExists = await _context.Departments
            .AnyAsync(d => d.Code.ToLower() == request.Code.Trim().ToLower());

        if (codeExists)
            return Conflict(new { message = "A department with this code already exists." });

        var department = new Department
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Description = request.Description?.Trim()
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDepartment),
            new { id = department.Id },
            department);
    }

    // PUT: api/departments/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDepartment(
        int id,
        UpdateDepartmentRequest request)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
            return NotFound(new { message = "Department not found." });

        var nameExists = await _context.Departments
            .AnyAsync(d => d.Id != id &&
                d.Name.ToLower() == request.Name.Trim().ToLower());

        if (nameExists)
            return Conflict(new { message = "Another department already uses this name." });

        var codeExists = await _context.Departments
            .AnyAsync(d => d.Id != id &&
                d.Code.ToLower() == request.Code.Trim().ToLower());

        if (codeExists)
            return Conflict(new { message = "Another department already uses this code." });

        department.Name = request.Name.Trim();
        department.Code = request.Code.Trim();
        department.Description = request.Description?.Trim();
        department.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return Ok(department);
    }

    // DELETE: api/departments/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
            return NotFound(new { message = "Department not found." });

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
