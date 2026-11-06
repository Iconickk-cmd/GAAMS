
using GAAMS.Domain.Entities;
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveBalancesController : ControllerBase
    {
        private readonly GAAMSDbContext _context;

        public LeaveBalancesController(GAAMSDbContext context)
        {
            _context = context;
        }

        // GET: api/LeaveBalances
        [HttpGet]
        public async Task<IActionResult> GetAllLeaveBalances()
        {
            var balances = await _context.LeaveBalances
                .Include(b => b.Employee)
                .OrderBy(b => b.Employee!.LastName)
                .ThenBy(b => b.LeaveType)
                .ThenBy(b => b.LeaveYear)
                .Select(b => new
                {
                    b.Id,
                    b.EmployeeId,
                    EmployeeNumber = b.Employee!.EmployeeNumber,
                    FirstName = b.Employee.FirstName,
                    LastName = b.Employee.LastName,
                    b.LeaveType,
                    b.LeaveYear,
                    b.EntitledDays,
                    b.AccruedDays,
                    b.TakenDays,
                    b.PendingDays,
                    b.RemainingDays,
                    b.CreatedAt,
                    b.UpdatedAt
                })
                .ToListAsync();

            return Ok(balances);
        }

        // GET: api/LeaveBalances/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLeaveBalance(int id)
        {
            var balance = await _context.LeaveBalances
                .Include(b => b.Employee)
                .Where(b => b.Id == id)
                .Select(b => new
                {
                    b.Id,
                    b.EmployeeId,
                    EmployeeNumber = b.Employee!.EmployeeNumber,
                    FirstName = b.Employee.FirstName,
                    LastName = b.Employee.LastName,
                    b.LeaveType,
                    b.LeaveYear,
                    b.EntitledDays,
                    b.AccruedDays,
                    b.TakenDays,
                    b.PendingDays,
                    b.RemainingDays,
                    b.CreatedAt,
                    b.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (balance == null)
            {
                return NotFound(new
                {
                    message = "Leave balance not found."
                });
            }

            return Ok(balance);
        }

        // GET: api/LeaveBalances/employee/2
        [HttpGet("employee/{employeeId:int}")]
        public async Task<IActionResult> GetEmployeeLeaveBalances(int employeeId)
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

            var balances = await _context.LeaveBalances
                .Where(b => b.EmployeeId == employeeId)
                .OrderBy(b => b.LeaveYear)
                .ThenBy(b => b.LeaveType)
                .Select(b => new
                {
                    b.Id,
                    b.EmployeeId,
                    EmployeeNumber = b.Employee!.EmployeeNumber,
                    FirstName = b.Employee.FirstName,
                    LastName = b.Employee.LastName,
                    b.LeaveType,
                    b.LeaveYear,
                    b.EntitledDays,
                    b.AccruedDays,
                    b.TakenDays,
                    b.PendingDays,
                    b.RemainingDays
                })
                .ToListAsync();

            return Ok(balances);
        }

        // GET: api/LeaveBalances/employee/2/year/2026
        [HttpGet("employee/{employeeId:int}/year/{year:int}")]
        public async Task<IActionResult> GetEmployeeLeaveBalancesByYear(
            int employeeId,
            int year)
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

            var balances = await _context.LeaveBalances
                .Where(b => b.EmployeeId == employeeId &&
                            b.LeaveYear == year)
                .OrderBy(b => b.LeaveType)
                .Select(b => new
                {
                    b.Id,
                    b.EmployeeId,
                    EmployeeNumber = b.Employee!.EmployeeNumber,
                    FirstName = b.Employee.FirstName,
                    LastName = b.Employee.LastName,
                    b.LeaveType,
                    b.LeaveYear,
                    b.EntitledDays,
                    b.AccruedDays,
                    b.TakenDays,
                    b.PendingDays,
                    b.RemainingDays
                })
                .ToListAsync();

            return Ok(balances);
        }
    }
}
