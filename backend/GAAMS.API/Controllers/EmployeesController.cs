
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

    // GET: api/Employees
    [HttpGet]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "lastName")
    {
        if (page < 1)
            return BadRequest(new { message = "Page must be greater than or equal to 1." });

        if (pageSize < 1 || pageSize > 100)
            return BadRequest(new { message = "Page size must be between 1 and 100." });

        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();

            query = query.Where(e =>
                e.FirstName.Contains(searchTerm) ||
                e.LastName.Contains(searchTerm) ||
                e.EmployeeNumber.Contains(searchTerm) ||
                (e.NationalId != null && e.NationalId.Contains(searchTerm)) ||
                (e.Email != null && e.Email.Contains(searchTerm)));
        }

        if (departmentId.HasValue)
        {
            if (departmentId.Value < 1)
                return BadRequest(new { message = "Department ID must be greater than or equal to 1." });

            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        switch (sortBy.Trim().ToLowerInvariant())
        {
            case "firstname":
                query = query.OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ThenBy(e => e.Id);
                break;

            case "employeenumber":
                query = query.OrderBy(e => e.EmployeeNumber)
                    .ThenBy(e => e.Id);
                break;

            case "dateofemployment":
                query = query.OrderBy(e => e.DateOfEmployment)
                    .ThenBy(e => e.Id);
                break;

            case "lastname":
                query = query.OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ThenBy(e => e.Id);
                break;

            default:
                return BadRequest(new
                {
                    message = "Invalid sortBy value. Use lastName, firstName, employeeNumber, or dateOfEmployment."
                });
        }

        var employees = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            employees
        });
    }

    // GET: api/Employees/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound();

        return Ok(employee);
    }

    // POST: api/Employees
    [HttpPost]
    public async Task<ActionResult<Employee>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request)
    {
        var employeeNumberExists = await _context.Employees
            .AnyAsync(e => e.EmployeeNumber == request.EmployeeNumber);

        if (employeeNumberExists)
            return Conflict(new
            {
                message = "An employee with this employee number already exists."
            });

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var nationalIdExists = await _context.Employees
                .AnyAsync(e => e.NationalId == request.NationalId);

            if (nationalIdExists)
                return Conflict(new
                {
                    message = "An employee with this National ID already exists."
                });
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == request.DepartmentId);

        if (!departmentExists)
            return BadRequest(new
            {
                message = "The selected department does not exist."
            });

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
            nameof(GetEmployee),
            new { id = employee.Id },
            employee);
    }

    // PUT: api/Employees/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(
        int id,
        [FromBody] UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound();

        if (employee.TerminationDate.HasValue)
        {
            return BadRequest(new
            {
                message = "A terminated employee cannot be reactivated through employee profile editing."
            });
        }

        var employeeNumberExists = await _context.Employees
            .AnyAsync(e =>
                e.EmployeeNumber == request.EmployeeNumber &&
                e.Id != id);

        if (employeeNumberExists)
            return Conflict(new
            {
                message = "Another employee already uses this employee number."
            });

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            var nationalIdExists = await _context.Employees
                .AnyAsync(e =>
                    e.NationalId == request.NationalId &&
                    e.Id != id);

            if (nationalIdExists)
                return Conflict(new
                {
                    message = "Another employee already uses this National ID."
                });
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == request.DepartmentId);

        if (!departmentExists)
            return BadRequest(new
            {
                message = "The selected department does not exist."
            });

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

    // POST: api/Employees/5/terminate
    [HttpPost("{id:int}/terminate")]
    public async Task<IActionResult> TerminateEmployee(
        int id,
        [FromBody] TerminateEmployeeRequest request)
    {
        if (request.TerminationDate == default)
            return BadRequest(new { message = "A valid termination date is required." });

        var terminationDate = request.TerminationDate.Date;

        if (terminationDate > DateTime.UtcNow.Date)
            return BadRequest(new { message = "The termination date cannot be in the future." });

        if (string.IsNullOrWhiteSpace(request.TerminationReason))
            return BadRequest(new { message = "A termination reason is required." });

        if (string.IsNullOrWhiteSpace(request.TerminationType))
            return BadRequest(new { message = "A termination type is required." });

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                await transaction.RollbackAsync();
                return NotFound(new { message = "Employee was not found." });
            }

            if (employee.TerminationDate.HasValue)
            {
                await transaction.RollbackAsync();
                return Conflict(new { message = "This employee has already been terminated." });
            }

            if (!employee.IsActive)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "This employee is already inactive." });
            }

            if (terminationDate < employee.DateOfEmployment.Date)
            {
                await transaction.RollbackAsync();
                return BadRequest(new
                {
                    message = "The termination date cannot be before the employment date."
                });
            }

            var currentAssignment =
                await _context.EmployeePositionHistories
                    .FirstOrDefaultAsync(h =>
                        h.EmployeeId == id && h.IsCurrent);

            if (currentAssignment != null &&
                terminationDate < currentAssignment.StartDate.Date)
            {
                await transaction.RollbackAsync();
                return BadRequest(new
                {
                    message = "The termination date cannot be before the current assignment start date."
                });
            }

            employee.IsActive = false;
            employee.TerminationDate =
                DateTime.SpecifyKind(terminationDate, DateTimeKind.Utc);
            employee.TerminationReason = request.TerminationReason.Trim();
            employee.TerminationType = request.TerminationType.Trim();

            if (currentAssignment != null)
            {
                currentAssignment.EndDate = terminationDate;
                currentAssignment.IsCurrent = false;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Employee employment terminated successfully.",
                employeeId = employee.Id,
                employeeNumber = employee.EmployeeNumber,
                employeeName = $"{employee.FirstName} {employee.LastName}",
                isActive = employee.IsActive,
                terminationDate = employee.TerminationDate,
                terminationType = employee.TerminationType,
                terminationReason = employee.TerminationReason,
                endedAssignment = currentAssignment != null
            });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new
            {
                message = "The termination could not be completed because of a database update conflict."
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // PUT: api/Employees/5/correct-termination-details
    [HttpPut("{id:int}/correct-termination-details")]
    public async Task<IActionResult> CorrectTerminationDetails(
        int id,
        [FromBody] CorrectTerminationDetailsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TerminationReason))
            return BadRequest(new { message = "A termination reason is required." });

        if (string.IsNullOrWhiteSpace(request.TerminationType))
            return BadRequest(new { message = "A termination type is required." });

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound(new { message = "Employee was not found." });

        if (employee.IsActive || !employee.TerminationDate.HasValue)
        {
            return BadRequest(new
            {
                message = "Only terminated employees can have their termination details corrected."
            });
        }

        employee.TerminationReason = request.TerminationReason.Trim();
        employee.TerminationType = request.TerminationType.Trim();

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Termination details corrected successfully.",
            employeeId = employee.Id,
            employeeNumber = employee.EmployeeNumber,
            employeeName = $"{employee.FirstName} {employee.LastName}",
            isActive = employee.IsActive,
            terminationDate = employee.TerminationDate,
            terminationReason = employee.TerminationReason,
            terminationType = employee.TerminationType
        });
    }

    // DELETE: api/Employees/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound();

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
