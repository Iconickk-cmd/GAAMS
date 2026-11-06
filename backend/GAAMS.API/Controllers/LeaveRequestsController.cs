
using GAAMS.API.DTOs;
using GAAMS.Application.Interfaces;
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
        private readonly ILeaveBalanceService _leaveBalanceService;

        public LeaveRequestsController(
            GAAMSDbContext context,
            ILeaveBalanceService leaveBalanceService)
        {
            _context = context;
            _leaveBalanceService = leaveBalanceService;
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
                    EmployeeName = l.Employee!.FirstName + " " +
                                   l.Employee.LastName,
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
        public async Task<IActionResult> GetEmployeeLeaveRequests(
            int employeeId)
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

            var startDate = request.StartDate.Date;
            var endDate = request.EndDate.Date;
            var leaveType = request.LeaveType.Trim();

            var overlappingRequest = await _context.LeaveRequests
                .AnyAsync(l =>
                    l.EmployeeId == request.EmployeeId &&
                    (l.Status == "Pending" || l.Status == "Approved") &&
                    startDate <= l.EndDate &&
                    endDate >= l.StartDate);

            if (overlappingRequest)
            {
                return Conflict(new
                {
                    message = "This employee already has a pending or approved leave request covering these dates."
                });
            }

            var totalDays = (endDate - startDate).Days + 1;

            // Split the leave days across calendar years.
            var yearlySegments = GetYearlyLeaveSegments(
                startDate,
                endDate);

            // Check the balance for every year before creating the request.
            foreach (var segment in yearlySegments)
            {
                decimal remainingDays;

                try
                {
                    remainingDays =
                        await _leaveBalanceService.GetRemainingDaysAsync(
                            request.EmployeeId,
                            leaveType,
                            segment.Year);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new
                    {
                        message = ex.Message,
                        leaveYear = segment.Year
                    });
                }

                if (remainingDays < segment.Days)
                {
                    return Conflict(new
                    {
                        message = "Insufficient leave balance.",
                        leaveType,
                        leaveYear = segment.Year,
                        requestedDays = segment.Days,
                        remainingDays
                    });
                }
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var leaveRequest = new LeaveRequest
                {
                    EmployeeId = request.EmployeeId,
                    LeaveType = leaveType,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalDays = totalDays,
                    Reason = request.Reason.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.LeaveRequests.Add(leaveRequest);

                await _context.SaveChangesAsync();

                // Reserve the requested days in each year's balance.
                foreach (var segment in yearlySegments)
                {
                    await _leaveBalanceService.UpdatePendingDaysAsync(
                        request.EmployeeId,
                        leaveType,
                        segment.Year,
                        segment.Days);
                }

                await transaction.CommitAsync();

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
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = ex.Message
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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

            // Validate the employee requesting leave when approving.
            if (request.Status == "Approved")
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e =>
                        e.Id == leaveRequest.EmployeeId);

                if (employee == null || !employee.IsActive)
                {
                    return BadRequest(new
                    {
                        message = "Leave cannot be approved for an inactive employee."
                    });
                }

                // Check for conflicting approved leave.
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

            // Validate the reviewing employee.
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

            var yearlySegments = GetYearlyLeaveSegments(
                leaveRequest.StartDate.Date,
                leaveRequest.EndDate.Date);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Update the request status.
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

                // Update the corresponding yearly balances.
                foreach (var segment in yearlySegments)
                {
                    if (request.Status == "Approved")
                    {
                        await _leaveBalanceService.ApproveLeaveAsync(
                            leaveRequest.EmployeeId,
                            leaveRequest.LeaveType,
                            segment.Year,
                            segment.Days);
                    }
                    else
                    {
                        await _leaveBalanceService.ReleasePendingDaysAsync(
                            leaveRequest.EmployeeId,
                            leaveRequest.LeaveType,
                            segment.Year,
                            segment.Days);
                    }
                }

                await transaction.CommitAsync();

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
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = ex.Message
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
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

            var yearlySegments = GetYearlyLeaveSegments(
                leaveRequest.StartDate.Date,
                leaveRequest.EndDate.Date);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                leaveRequest.Status = "Cancelled";
                leaveRequest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Release the reserved days from each year's balance.
                foreach (var segment in yearlySegments)
                {
                    await _leaveBalanceService.ReleasePendingDaysAsync(
                        leaveRequest.EmployeeId,
                        leaveRequest.LeaveType,
                        segment.Year,
                        segment.Days);
                }

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Leave request cancelled successfully.",
                    leaveRequest.Id,
                    leaveRequest.Status
                });
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();

                return Conflict(new
                {
                    message = ex.Message
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Splits a leave request into calendar-year segments.
        private static List<YearlyLeaveSegment> GetYearlyLeaveSegments(
            DateTime startDate,
            DateTime endDate)
        {
            var segments = new List<YearlyLeaveSegment>();

            var currentStart = startDate.Date;

            while (currentStart <= endDate.Date)
            {
                var year = currentStart.Year;

                var yearEnd = new DateTime(
                    year,
                    12,
                    31);

                var segmentEnd = endDate.Date < yearEnd
                    ? endDate.Date
                    : yearEnd;

                var days = (segmentEnd - currentStart).Days + 1;

                segments.Add(new YearlyLeaveSegment
                {
                    Year = year,
                    Days = days
                });

                currentStart = segmentEnd.AddDays(1);
            }

            return segments;
        }

        private class YearlyLeaveSegment
        {
            public int Year { get; set; }
            public decimal Days { get; set; }
        }
    }
}
