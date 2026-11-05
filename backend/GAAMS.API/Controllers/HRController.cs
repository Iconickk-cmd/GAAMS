
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.API.Controllers
{
    [Route("api/hr")]
    [ApiController]
    public class HRController : ControllerBase
    {
        private readonly GAAMSDbContext _context;

        public HRController(GAAMSDbContext context)
        {
            _context = context;
        }

        // GET: api/hr/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var totalEmployees = await _context.Employees
                .AsNoTracking()
                .CountAsync();

            var activeEmployees = await _context.Employees
                .AsNoTracking()
                .CountAsync(e => e.IsActive);

            var inactiveEmployees = await _context.Employees
                .AsNoTracking()
                .CountAsync(e => !e.IsActive);

            var totalDepartments = await _context.Departments
                .AsNoTracking()
                .CountAsync();

            var activeDepartments = await _context.Departments
                .AsNoTracking()
                .CountAsync(d => d.IsActive);

            return Ok(new
            {
                totalEmployees,
                activeEmployees,
                inactiveEmployees,
                totalDepartments,
                activeDepartments
            });
        }
    }
}
