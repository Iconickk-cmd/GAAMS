
using GAAMS.API.DTOs;
using GAAMS.Domain.Entities;
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly GAAMSDbContext _context;

        public LeaveRequestsController(GAAMSDbContext context)
        {
            _context = context;
        }

        // GET: api/LeaveRequests
        [HttpGet]
        public async Task<IActionResult> GetLeaveRequests()
        {
            var requests = await _context.LeaveRequests
                .AsNoTracking()
                .Include(l => l.Employee)
                .Include(l => l.ReviewedByEmployee)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.EmployeeId,
                    EmployeeName = l.Employee!.FirstName + " " + l.Employee.LastName,
                    l.LeaveType,
                    l.StartDate,
                    l.EndDate,
                    l.TotalDays,
                    l.Reason,
                    l.Status,
                    l.ReviewedByEmployeeId,
                    ReviewerName = l.ReviewedByEmployee == null
                        ? null
                        : l.ReviewedByEmployee.FirstName + " " +
                          l.ReviewedByEmployee.LastName,
                    l.ReviewedAt,
                    l.ReviewComments,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        // GET: api/LeaveRequests/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLeaveRequest(int id)
        {
            var request = await _context.LeaveRequests
                .AsNoTracking()
                .Include(l => l.Employee)
                .Include(l => l.ReviewedByEmployee)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (request == null)
            {
                return NotFound(new
                {
                    message = "Leave request not found."
                });
            }

            return Ok(new
            {
                request.Id,
                request.EmployeeId,
                EmployeeName = request.Employee!.FirstName + " " +
                               request.Employee.LastName,
                request.LeaveType,
                request.StartDate,
                request.EndDate,
                request.TotalDays,
                request.Reason,
                request.Status,
                request.ReviewedByEmployeeId,
                ReviewerName = request.ReviewedByEmployee == null
                    ? null
                    : request.ReviewedByEmployee.FirstName + " " +
                      request.ReviewedByEmployee.LastName,
                request.ReviewedAt,
                request.ReviewComments,
                request.CreatedAt
            });
        }

        // GET: api/LeaveRequests/employee/2
        [HttpGet("employee/{employeeId:int}")]
        public async Task<IActionResult> GetEmployeeLeaveRequests(int employeeId)
        {
            var employeeExists = await _context.Employees
                .AnyAsync(e => e.Id == employeeId);

            if (!employeeExists)
            {
                return NotFound(new
                {
                    message = "Employee not found."
                });
            }

            var requests = await _context.LeaveRequests
                .AsNoTracking()
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.EmployeeId,
                    l.LeaveType,
                    l.StartDate,
                    l.EndDate,
                    l.TotalDays,
                    l.Reason,
                    l.Status,
                    l.ReviewedAt,
                    l.ReviewComments,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        // POST: api/LeaveRequests
        [HttpPost]
        public async Task<IActionResult> CreateLeaveRequest(
            [FromBody] CreateLeaveRequest request)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

            if (employee == null)
            {
                return BadRequest(new
                {
                    message = "The selected employee does not exist."
                });
            }

            if (!employee.IsActive)
            {
                return BadRequest(new
                {
                    message = "Inactive employees cannot submit leave requests."
                });
            }

            if (request.StartDate == default ||
                request.EndDate == default)
            {
                return BadRequest(new
                {
                    message = "Valid start and end dates are required."
                });
            }

            if (request.StartDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new
                {
                    message = "Leave cannot start in the past."
                });
            }

            if (request.EndDate.Date < request.StartDate.Date)
            {
                return BadRequest(new
                {
                    message = "The end date cannot be before the start date."
                });
            }

            if (string.IsNullOrWhiteSpace(request.LeaveType) ||
                string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new
                {
                    message = "Leave type and reason are required."
                });
            }

            var overlappingRequest = await _context.LeaveRequests
                .AnyAsync(l =>
                    l.EmployeeId == request.EmployeeId &&
                    (l.Status == "Pending" || l.Status == "Approved") &&
                    request.StartDate.Date <= l.EndDate &&
                    request.EndDate.Date >= l.StartDate);

            if (overlappingRequest)
            {
                return Conflict(new
                {
                    message = "This employee already has a pending or approved leave request covering these dates."
                });
            }

            var leaveRequest = new LeaveRequest
            {
                EmployeeId = request.EmployeeId,
                LeaveType = request.LeaveType.Trim(),
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate.Date,
                TotalDays = (request.EndDate.Date -
                             request.StartDate.Date).Days + 1,
                Reason = request.Reason.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(leaveRequest);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetLeaveRequest),
                new { id = leaveRequest.Id },
                new
                {
                    message = "Leave request submitted successfully.",
                    leaveRequest.Id,
                    leaveRequest.EmployeeId,
                    leaveRequest.LeaveType,
                    leaveRequest.StartDate,
                    leaveRequest.EndDate,
                    leaveRequest.TotalDays,
                    leaveRequest.Status,
                    leaveRequest.CreatedAt
                });
        }

        // PUT: api/LeaveRequests/5/review
        [HttpPut("{id:int}/review")]
        public async Task<IActionResult> ReviewLeaveRequest(
            int id,
            [FromBody] ReviewLeaveRequest request)
        {
            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leaveRequest == null)
            {
                return NotFound(new
                {
                    message = "Leave request not found."
                });
            }

            if (leaveRequest.Status != "Pending")
            {
                return BadRequest(new
                {
                    message = "Only pending leave requests can be reviewed."
                });
            }

            if (request.Status != "Approved" &&
                request.Status != "Rejected")
            {
                return BadRequest(new
                {
                    message = "Status must be Approved or Rejected."
                });
            }

            var reviewer = await _context.Employees
                .FirstOrDefaultAsync(e =>
                    e.Id == request.ReviewedByEmployeeId);

            if (reviewer == null || !reviewer.IsActive)
            {
                return BadRequest(new
                {
                    message = "A valid active reviewing employee is required."
                });
            }

            if (request.Status == "Approved")
            {
                var conflictingRequest = await _context.LeaveRequests
                    .AnyAsync(l =>
                        l.Id != id &&
                        l.EmployeeId == leaveRequest.EmployeeId &&
                        l.Status == "Approved" &&
                        leaveRequest.StartDate <= l.EndDate &&
                        leaveRequest.EndDate >= l.StartDate);

                if (conflictingRequest)
                {
                    return Conflict(new
                    {
                        message = "The employee already has approved leave covering these dates."
                    });
                }
            }

            leaveRequest.Status = request.Status;
            leaveRequest.ReviewedByEmployeeId =
                request.ReviewedByEmployeeId;
            leaveRequest.ReviewedAt = DateTime.UtcNow;
            leaveRequest.ReviewComments =
                string.IsNullOrWhiteSpace(request.ReviewComments)
                    ? null
                    : request.ReviewComments.Trim();
            leaveRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Leave request {request.Status.ToLower()} successfully.",
                leaveRequest.Id,
                leaveRequest.Status,
                leaveRequest.ReviewedByEmployeeId,
                leaveRequest.ReviewedAt,
                leaveRequest.ReviewComments
            });
        }

        // DELETE: api/LeaveRequests/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> CancelLeaveRequest(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leaveRequest == null)
            {
                return NotFound(new
                {
                    message = "Leave request not found."
                });
            }

            if (leaveRequest.Status != "Pending")
            {
                return BadRequest(new
                {
                    message = "Only pending leave requests can be cancelled."
                });
            }

            leaveRequest.Status = "Cancelled";
            leaveRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Leave request cancelled successfully.",
                leaveRequest.Id,
                leaveRequest.Status
            });
        }
    }
}
