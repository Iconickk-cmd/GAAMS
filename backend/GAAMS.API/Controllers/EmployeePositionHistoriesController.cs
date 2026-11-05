
using GAAMS.Domain.Entities;
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeePositionHistoriesController : ControllerBase
{
    private readonly GAAMSDbContext _context;

    public EmployeePositionHistoriesController(GAAMSDbContext context)
    {
        _context = context;
    }

    // GET: api/EmployeePositionHistories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeePositionHistory>>> GetAll()
    {
        var histories = await _context.EmployeePositionHistories
            .Include(h => h.Employee)
            .Include(h => h.Position)
                .ThenInclude(p => p!.Department)
            .OrderByDescending(h => h.IsCurrent)
            .ThenByDescending(h => h.StartDate)
            .ToListAsync();

        return Ok(histories);
    }

    // GET: api/EmployeePositionHistories/employee/2
    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<IEnumerable<EmployeePositionHistory>>>
        GetEmployeeHistory(int employeeId)
    {
        var histories = await _context.EmployeePositionHistories
            .Include(h => h.Employee)
            .Include(h => h.Position)
                .ThenInclude(p => p!.Department)
            .Where(h => h.EmployeeId == employeeId)
            .OrderByDescending(h => h.StartDate)
            .ToListAsync();

        return Ok(histories);
    }

    // GET: api/EmployeePositionHistories/position/1
    [HttpGet("position/{positionId}")]
    public async Task<ActionResult<IEnumerable<EmployeePositionHistory>>>
        GetPositionEmployees(int positionId)
    {
        var histories = await _context.EmployeePositionHistories
            .Include(h => h.Employee)
            .Include(h => h.Position)
            .Where(h => h.PositionId == positionId)
            .OrderByDescending(h => h.StartDate)
            .ToListAsync();

        return Ok(histories);
    }

    // POST: api/EmployeePositionHistories
    [HttpPost]
    public async Task<ActionResult<EmployeePositionHistory>> AssignEmployee(
        [FromBody] CreatePositionAssignmentRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

        if (employee == null)
            return NotFound(new { message = "Employee was not found." });

        if (!employee.IsActive)
            return BadRequest(new { message = "This employee is inactive." });

        var position = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == request.PositionId);

        if (position == null)
            return NotFound(new { message = "Position was not found." });

        if (!position.IsActive)
            return BadRequest(new { message = "This position is inactive." });

        var alreadyAssigned = await _context.EmployeePositionHistories
            .AnyAsync(h => h.EmployeeId == request.EmployeeId && h.IsCurrent);

        if (alreadyAssigned)
            return Conflict(new
            {
                message = "This employee already has a current position assignment."
            });

        var currentOccupancy = await _context.EmployeePositionHistories
            .CountAsync(h => h.PositionId == request.PositionId && h.IsCurrent);

        if (currentOccupancy >= position.ApprovedHeadcount)
            return Conflict(new
            {
                message = "This position has reached its approved headcount."
            });

        if (request.StartDate.Date > DateTime.UtcNow.Date)
            return BadRequest(new
            {
                message = "The assignment start date cannot be in the future."
            });

        var assignment = new EmployeePositionHistory
        {
            EmployeeId = request.EmployeeId,
            PositionId = request.PositionId,
            StartDate = request.StartDate.Date,
            ChangeReason = string.IsNullOrWhiteSpace(request.ChangeReason)
                ? "Initial appointment"
                : request.ChangeReason.Trim(),
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeePositionHistories.Add(assignment);

        // Keep the employee profile synchronized with the assigned position.
        employee.JobTitle = position.Title;
        employee.DepartmentId = position.DepartmentId;

        await _context.SaveChangesAsync();

        return Ok(assignment);
    }

    // POST: api/EmployeePositionHistories/transfer
    [HttpPost("transfer")]
    public async Task<ActionResult<EmployeePositionHistory>> TransferEmployee(
        [FromBody] TransferEmployeeRequest request)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

        if (employee == null)
            return NotFound(new { message = "Employee was not found." });

        if (!employee.IsActive)
            return BadRequest(new { message = "This employee is inactive." });

        var newPosition = await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == request.NewPositionId);

        if (newPosition == null)
            return NotFound(new { message = "Destination position was not found." });

        if (!newPosition.IsActive)
            return BadRequest(new
            {
                message = "The destination position is inactive."
            });

        var currentAssignment = await _context.EmployeePositionHistories
            .FirstOrDefaultAsync(h =>
                h.EmployeeId == request.EmployeeId && h.IsCurrent);

        if (currentAssignment == null)
            return BadRequest(new
            {
                message = "This employee does not have a current position assignment."
            });

        if (currentAssignment.PositionId == request.NewPositionId)
            return BadRequest(new
            {
                message = "The employee is already assigned to this position."
            });

        if (request.EffectiveDate.Date < currentAssignment.StartDate.Date)
            return BadRequest(new
            {
                message = "The transfer date cannot be before the current assignment start date."
            });

        if (request.EffectiveDate.Date > DateTime.UtcNow.Date)
            return BadRequest(new
            {
                message = "The transfer date cannot be in the future."
            });

        var newPositionOccupancy = await _context.EmployeePositionHistories
            .CountAsync(h =>
                h.PositionId == request.NewPositionId && h.IsCurrent);

        if (newPositionOccupancy >= newPosition.ApprovedHeadcount)
            return Conflict(new
            {
                message = "The destination position has reached its approved headcount."
            });

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // End the employee's existing assignment.
            currentAssignment.EndDate = request.EffectiveDate.Date;
            currentAssignment.IsCurrent = false;

            // Create the new position assignment.
            var newAssignment = new EmployeePositionHistory
            {
                EmployeeId = request.EmployeeId,
                PositionId = request.NewPositionId,
                StartDate = request.EffectiveDate.Date,
                ChangeReason = string.IsNullOrWhiteSpace(request.ChangeReason)
                    ? "Position transfer"
                    : request.ChangeReason.Trim(),
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmployeePositionHistories.Add(newAssignment);

            // Update the employee's main profile.
            employee.JobTitle = newPosition.Title;
            employee.DepartmentId = newPosition.DepartmentId;

            // Save all changes together.
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(newAssignment);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            return Conflict(new
            {
                message = "The transfer could not be completed. Please check the employee's current assignment and destination position."
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // PUT: api/EmployeePositionHistories/1/end
    [HttpPut("{id}/end")]
    public async Task<IActionResult> EndAssignment(
        int id,
        [FromBody] EndPositionAssignmentRequest request)
    {
        var assignment = await _context.EmployeePositionHistories
            .FirstOrDefaultAsync(h => h.Id == id);

        if (assignment == null)
            return NotFound(new { message = "Assignment was not found." });

        if (!assignment.IsCurrent)
            return BadRequest(new
            {
                message = "This assignment has already ended."
            });

        if (request.EndDate.Date < assignment.StartDate.Date)
            return BadRequest(new
            {
                message = "The end date cannot be before the assignment start date."
            });

        if (request.EndDate.Date > DateTime.UtcNow.Date)
            return BadRequest(new
            {
                message = "The end date cannot be in the future."
            });

        assignment.EndDate = request.EndDate.Date;
        assignment.IsCurrent = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Employee assignment ended successfully.",
            assignment
        });
    }
}

// Request used when assigning an employee to a position.
public class CreatePositionAssignmentRequest
{
    public int EmployeeId { get; set; }
    public int PositionId { get; set; }
    public DateTime StartDate { get; set; }
    public string? ChangeReason { get; set; }
}

// Request used when transferring an employee.
public class TransferEmployeeRequest
{
    public int EmployeeId { get; set; }
    public int NewPositionId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public string? ChangeReason { get; set; }
}

// Request used when ending an assignment.
public class EndPositionAssignmentRequest
{
    public DateTime EndDate { get; set; }
}
