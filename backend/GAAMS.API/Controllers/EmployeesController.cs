using Microsoft.EntityFrameworkCore;
using Npgsql;
using GAAMS.API.DTOs;
using GAAMS.Domain.Entities;
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace GAAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly GAAMSDbContext _context;

    public EmployeesController(GAAMSDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        var employees = await _context.Employees
            .Include(e => e.Department)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync();

        return Ok(employees);
    }
        [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(
        CreateEmployeeRequest request)
    {
        var employeeNumberExists = await _context.Employees
            .AnyAsync(e => e.EmployeeNumber == request.EmployeeNumber);

        if (employeeNumberExists)
        {
            return Conflict(new
            {
                message = "An employee with this employee number already exists."
            });
        }

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var nationalIdExists = await _context.Employees
                .AnyAsync(e => e.NationalId == request.NationalId);

            if (nationalIdExists)
            {
                return Conflict(new
                {
                    message = "An employee with this National ID already exists."
                });
            }
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == request.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message = "The selected department does not exist."
            });
        }

        var employee = new Employee
        {
            EmployeeNumber = request.EmployeeNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            NationalId = request.NationalId,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            DateOfEmployment = request.DateOfEmployment,
            JobTitle = request.JobTitle,
            DepartmentId = request.DepartmentId
        };

        _context.Employees.Add(employee);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEmployees),
            new { id = employee.Id },
            employee);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(
    int id,
    UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound();
        }
        var employeeNumberExists = await _context.Employees
    .AnyAsync(e =>
        e.EmployeeNumber == request.EmployeeNumber &&
        e.Id != id);

        if (employeeNumberExists)
        {
            return Conflict(new
            {
                message = "Another employee already uses this employee number."
            });
        }

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var nationalIdExists = await _context.Employees
                .AnyAsync(e =>
                    e.NationalId == request.NationalId &&
                    e.Id != id);

            if (nationalIdExists)
            {
                return Conflict(new
                {
                    message = "Another employee already uses this National ID."
                });
            }
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == request.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message = "The selected department does not exist."
            });
        }

        employee.EmployeeNumber = request.EmployeeNumber;
        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.NationalId = request.NationalId;
        employee.Email = request.Email;
        employee.PhoneNumber = request.PhoneNumber;
        employee.DateOfBirth = request.DateOfBirth;
        employee.DateOfEmployment = request.DateOfEmployment;
        employee.JobTitle = request.JobTitle;
        employee.DepartmentId = request.DepartmentId;
        employee.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        var updatedEmployee = await _context.Employees
    .Include(e => e.Department)
    .FirstAsync(e => e.Id == id);

        return Ok(updatedEmployee);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            return NotFound();
        }

        _context.Employees.Remove(employee);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}