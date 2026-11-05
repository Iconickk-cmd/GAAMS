
using GAAMS.Domain.Entities;
using GAAMS.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAAMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PositionsController : ControllerBase
{
    private readonly GAAMSDbContext _context;

    public PositionsController(GAAMSDbContext context)
    {
        _context = context;
    }

    // GET: api/Positions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetPositions()
    {
        var positions = await _context.Positions
            .Include(p => p.Department)
            .OrderBy(p => p.Department!.Name)
            .ThenBy(p => p.Title)
            .Select(p => new
            {
                p.Id,
                p.PositionCode,
                p.Title,
                p.Description,
                p.DepartmentId,
                Department = p.Department,
                p.EmploymentType,
                p.EmploymentGrade,
                p.ApprovedHeadcount,
                p.IsActive,
                p.CreatedAt,
                CurrentOccupancy = _context.EmployeePositionHistories
                    .Count(h => h.PositionId == p.Id && h.IsCurrent)
            })
            .ToListAsync();

        return Ok(positions);
    }

    // GET: api/Positions/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetPosition(int id)
    {
        var position = await _context.Positions
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.PositionCode,
                p.Title,
                p.Description,
                p.DepartmentId,
                Department = p.Department,
                p.EmploymentType,
                p.EmploymentGrade,
                p.ApprovedHeadcount,
                p.IsActive,
                p.CreatedAt,
                CurrentOccupancy = _context.EmployeePositionHistories
                    .Count(h => h.PositionId == p.Id && h.IsCurrent)
            })
            .FirstOrDefaultAsync();

        if (position == null)
        {
            return NotFound(new { message = "Position not found." });
        }

        return Ok(position);
    }

    // POST: api/Positions
    [HttpPost]
    public async Task<ActionResult<Position>> CreatePosition(Position position)
    {
        if (string.IsNullOrWhiteSpace(position.PositionCode) ||
            string.IsNullOrWhiteSpace(position.Title))
        {
            return BadRequest(new
            {
                message = "Position code and title are required."
            });
        }

        if (position.ApprovedHeadcount < 1)
        {
            return BadRequest(new
            {
                message = "Approved headcount must be at least 1."
            });
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == position.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message = "The selected department does not exist."
            });
        }

        var code = position.PositionCode.Trim();

        var codeExists = await _context.Positions
            .AnyAsync(p => p.PositionCode == code);

        if (codeExists)
        {
            return Conflict(new
            {
                message = "A position with this code already exists."
            });
        }

        position.PositionCode = code;
        position.Title = position.Title.Trim();
        position.CreatedAt = DateTime.UtcNow;

        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPosition),
            new { id = position.Id },
            position);
    }

    // PUT: api/Positions/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePosition(
        int id,
        Position updatedPosition)
    {
        var position = await _context.Positions.FindAsync(id);

        if (position == null)
        {
            return NotFound(new { message = "Position not found." });
        }

        if (string.IsNullOrWhiteSpace(updatedPosition.PositionCode) ||
            string.IsNullOrWhiteSpace(updatedPosition.Title))
        {
            return BadRequest(new
            {
                message = "Position code and title are required."
            });
        }

        if (updatedPosition.ApprovedHeadcount < 1)
        {
            return BadRequest(new
            {
                message = "Approved headcount must be at least 1."
            });
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.Id == updatedPosition.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message = "The selected department does not exist."
            });
        }

        var code = updatedPosition.PositionCode.Trim();

        var duplicateCode = await _context.Positions
            .AnyAsync(p => p.Id != id && p.PositionCode == code);

        if (duplicateCode)
        {
            return Conflict(new
            {
                message = "Another position already uses this code."
            });
        }

        position.PositionCode = code;
        position.Title = updatedPosition.Title.Trim();
        position.Description = updatedPosition.Description;
        position.DepartmentId = updatedPosition.DepartmentId;
        position.EmploymentType = updatedPosition.EmploymentType;
        position.EmploymentGrade = updatedPosition.EmploymentGrade;
        position.ApprovedHeadcount = updatedPosition.ApprovedHeadcount;
        position.IsActive = updatedPosition.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/Positions/5/deactivate
    [HttpPatch("{id:int}/deactivate")]
    public async Task<IActionResult> DeactivatePosition(int id)
    {
        var position = await _context.Positions.FindAsync(id);

        if (position == null)
        {
            return NotFound(new { message = "Position not found." });
        }

        var hasCurrentEmployees = await _context.EmployeePositionHistories
            .AnyAsync(h => h.PositionId == id && h.IsCurrent);

        if (hasCurrentEmployees)
        {
            return BadRequest(new
            {
                message = "This position has current employees assigned. Reassign them before deactivating it."
            });
        }

        position.IsActive = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
