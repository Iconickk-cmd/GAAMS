
using GAAMS.API.DTOs;
using GAAMS.Domain.Entities;
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeSeparationsController : ControllerBase
{
    private readonly GAAMSDbContext _context;

    private static readonly string[] AllowedSeparationTypes =
    {
        "Resignation",
        "Retirement",
        "Dismissal",
        "Redundancy",
        "End of contract",
        "Death",
        "Medical incapacity",
        "Other"
    };

    public EmployeeSeparationsController(GAAMSDbContext context)
    {
        _context = context;
    }

    // GET: api/EmployeeSeparations
    [HttpGet]
    public async Task<IActionResult> GetSeparations()
    {
        var separations = await _context.EmployeeSeparations
            .Include(s => s.Employee)
            .Include(s => s.Clearances)
            .OrderByDescending(s => s.RequestedAt)
            .ToListAsync();

        return Ok(separations);
    }

    // GET: api/EmployeeSeparations/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSeparation(int id)
    {
        var separation = await _context.EmployeeSeparations
            .Include(s => s.Employee)
            .Include(s => s.Clearances)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (separation == null)
        {
            return NotFound(new
            {
                message = "Employee separation request not found."
            });
        }

        return Ok(separation);
    }

    // POST: api/EmployeeSeparations
    // Submit a new separation request.
    [HttpPost]
    public async Task<IActionResult> CreateSeparation(
        [FromBody] CreateEmployeeSeparationRequest request)
    {
        if (!AllowedSeparationTypes.Contains(
                request.SeparationType,
                StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Invalid separation type.",
                allowedTypes = AllowedSeparationTypes
            });
        }

        if (request.ProposedLastWorkingDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new
            {
                message = "The proposed last working date cannot be in the past."
            });
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

        if (employee == null)
        {
            return NotFound(new
            {
                message = "Employee not found."
            });
        }

        if (!employee.IsActive || employee.TerminationDate != null)
        {
            return BadRequest(new
            {
                message = "A separation request cannot be submitted for an inactive or terminated employee."
            });
        }

        var existingRequest = await _context.EmployeeSeparations
            .AnyAsync(s =>
                s.EmployeeId == request.EmployeeId &&
                (s.Status == "PendingApproval" ||
                 s.Status == "Approved" ||
                 s.Status == "PendingClearance"));

        if (existingRequest)
        {
            return Conflict(new
            {
                message = "This employee already has an active separation request."
            });
        }

        var separation = new EmployeeSeparation
        {
            EmployeeId = request.EmployeeId,
            SeparationType = AllowedSeparationTypes.First(
                t => t.Equals(
                    request.SeparationType,
                    StringComparison.OrdinalIgnoreCase)),
            Reason = request.Reason.Trim(),
            ProposedLastWorkingDate = request.ProposedLastWorkingDate.Date,
            RequestedAt = DateTime.UtcNow,
            RequestedBy = request.RequestedBy.Trim(),
            Status = "PendingApproval",
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeSeparations.Add(separation);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "An active separation request already exists for this employee."
            });
        }

        return CreatedAtAction(
            nameof(GetSeparation),
            new { id = separation.Id },
            new
            {
                message = "Employee separation request submitted successfully.",
                separation
            });
    }

    // POST: api/EmployeeSeparations/1/review
    // Management approves or rejects a request.
    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> ReviewSeparation(
        int id,
        [FromBody] ReviewEmployeeSeparationRequest request)
    {
        var separation = await _context.EmployeeSeparations
            .Include(s => s.Clearances)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (separation == null)
        {
            return NotFound(new
            {
                message = "Employee separation request not found."
            });
        }

        if (separation.Status != "PendingApproval")
        {
            return BadRequest(new
            {
                message = "Only requests awaiting management approval can be reviewed."
            });
        }

        if (request.Approved && !request.ClearanceDepartments.Any())
        {
            return BadRequest(new
            {
                message = "At least one clearance department must be specified when approving a separation."
            });
        }

        var departments = request.ClearanceDepartments
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (request.Approved && departments.Count == 0)
        {
            return BadRequest(new
            {
                message = "Please provide valid clearance department names."
            });
        }

        if (request.Approved && departments.Any(d => d.Length > 100))
        {
            return BadRequest(new
            {
                message = "Clearance department names cannot exceed 100 characters."
            });
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        separation.DecisionBy = request.DecisionBy.Trim();
        separation.DecisionAt = DateTime.UtcNow;
        separation.DecisionComments = request.DecisionComments?.Trim();

        if (request.Approved)
        {
            separation.Status = "PendingClearance";

            foreach (var department in departments)
            {
                separation.Clearances.Add(
                    new EmployeeSeparationClearance
                    {
                        DepartmentName = department,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    });
            }
        }
        else
        {
            separation.Status = "Rejected";
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = request.Approved
                ? "Separation request approved. Departmental clearances have been created."
                : "Separation request rejected.",
            separationId = separation.Id,
            status = separation.Status,
            decisionBy = separation.DecisionBy,
            decisionAt = separation.DecisionAt,
            clearances = separation.Clearances
        });
    }

    // PUT: api/EmployeeSeparations/1/clearances/1
    // A department records its clearance.
    [HttpPut("{id:int}/clearances/{clearanceId:int}")]
    public async Task<IActionResult> CompleteClearance(
        int id,
        int clearanceId,
        [FromBody] CompleteEmployeeSeparationClearanceRequest request)
    {
        var separation = await _context.EmployeeSeparations
            .FirstOrDefaultAsync(s => s.Id == id);

        if (separation == null)
        {
            return NotFound(new
            {
                message = "Employee separation request not found."
            });
        }

        if (separation.Status != "PendingClearance")
        {
            return BadRequest(new
            {
                message = "Clearances can only be completed for approved requests awaiting clearance."
            });
        }

        if (!request.IsCleared)
        {
            return BadRequest(new
            {
                message = "This endpoint records completed clearances only. Set IsCleared to true."
            });
        }

        var clearance = await _context.EmployeeSeparationClearances
            .FirstOrDefaultAsync(c =>
                c.Id == clearanceId &&
                c.EmployeeSeparationId == id);

        if (clearance == null)
        {
            return NotFound(new
            {
                message = "The specified departmental clearance was not found."
            });
        }

        if (clearance.Status != "Pending")
        {
            return BadRequest(new
            {
                message = "This clearance has already been processed."
            });
        }

        clearance.Status = "Completed";
        clearance.Remarks = request.Remarks?.Trim();
        clearance.ClearedBy = request.ClearedBy.Trim();
        clearance.ClearedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var remainingClearances = await _context.EmployeeSeparationClearances
            .CountAsync(c =>
                c.EmployeeSeparationId == id &&
                c.Status == "Pending");

        return Ok(new
        {
            message = "Departmental clearance completed successfully.",
            clearanceId = clearance.Id,
            department = clearance.DepartmentName,
            status = clearance.Status,
            clearedBy = clearance.ClearedBy,
            clearedAt = clearance.ClearedAt,
            remainingClearances
        });
    }

    // POST: api/EmployeeSeparations/1/finalize
    // Finalize separation after all departmental clearances.
    [HttpPost("{id:int}/finalize")]
    public async Task<IActionResult> FinalizeSeparation(
        int id,
        [FromBody] FinalizeEmployeeSeparationRequest request)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var separation = await _context.EmployeeSeparations
            .Include(s => s.Employee)
            .Include(s => s.Clearances)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (separation == null)
        {
            return NotFound(new
            {
                message = "Employee separation request not found."
            });
        }

        if (separation.Status != "PendingClearance")
        {
            return BadRequest(new
            {
                message = "Only approved separation requests awaiting clearance can be finalized."
            });
        }

        if (!separation.Clearances.Any())
        {
            return BadRequest(new
            {
                message = "No departmental clearances have been created."
            });
        }

        var incompleteClearances = separation.Clearances
            .Where(c => c.Status != "Completed" &&
                        c.Status != "NotRequired")
            .Select(c => c.DepartmentName)
            .ToList();

        if (incompleteClearances.Any())
        {
            return BadRequest(new
            {
                message = "All required departmental clearances must be completed before finalization.",
                incompleteDepartments = incompleteClearances
            });
        }

        var employee = separation.Employee;

        if (employee == null)
        {
            return NotFound(new
            {
                message = "The employee associated with this request could not be found."
            });
        }

        if (!employee.IsActive || employee.TerminationDate != null)
        {
            return BadRequest(new
            {
                message = "This employee has already been terminated."
            });
        }

        // Finalization is the only operation that terminates the employee.
        employee.IsActive = false;
        employee.TerminationDate = DateTime.SpecifyKind(
            separation.ProposedLastWorkingDate.Date,
            DateTimeKind.Utc);
        employee.TerminationReason = separation.Reason;
        employee.TerminationType = separation.SeparationType;

        // Close any current position assignment.
        var currentAssignment = await _context.EmployeePositionHistories
            .FirstOrDefaultAsync(h =>
                h.EmployeeId == employee.Id &&
                h.IsCurrent);

        var endedAssignment = false;

        if (currentAssignment != null)
        {
            currentAssignment.IsCurrent = false;
            currentAssignment.EndDate =
                separation.ProposedLastWorkingDate.Date;

            endedAssignment = true;
        }

        separation.Status = "Finalized";
        separation.FinalizedBy = request.FinalizedBy.Trim();
        separation.FinalizedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            message = "Employee separation finalized successfully.",
            separationId = separation.Id,
            employeeId = employee.Id,
            employeeNumber = employee.EmployeeNumber,
            employeeName = $"{employee.FirstName} {employee.LastName}",
            isActive = employee.IsActive,
            terminationDate = employee.TerminationDate,
            terminationReason = employee.TerminationReason,
            terminationType = employee.TerminationType,
            endedAssignment,
            status = separation.Status,
            finalizedBy = separation.FinalizedBy,
            finalizedAt = separation.FinalizedAt
        });
    }
}
