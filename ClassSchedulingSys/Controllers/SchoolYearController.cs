// ClassSchedulingSys/Controllers/SchoolYearController
using ClassSchedulingSys.Data;
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")]
    public class SchoolYearController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SchoolYearController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SchoolYearDto>>> GetAll()
        {
            var items = await _context.SchoolYears
                .OrderByDescending(y => y.StartYear)
                .Select(y => new SchoolYearDto
                {
                    Id = y.Id,
                    StartYear = y.StartYear,
                    EndYear = y.EndYear,
                    IsCurrent = y.IsCurrent,
                    IsArchived = y.IsArchived
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSchoolYearDto dto)
        {
            if (dto.EndYear <= dto.StartYear)
                return BadRequest("End year must be greater than start year.");

            var exists = await _context.SchoolYears.AnyAsync(y =>
                y.StartYear == dto.StartYear && y.EndYear == dto.EndYear);

            if (exists)
                return Conflict("This school year already exists.");

            var sy = new SchoolYear
            {
                StartYear = dto.StartYear,
                EndYear = dto.EndYear
            };

            _context.SchoolYears.Add(sy);
            await _context.SaveChangesAsync();

            return Ok(new { message = "School year added successfully." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSchoolYearDto dto)
        {
            var sy = await _context.SchoolYears.FindAsync(id);
            if (sy == null) return NotFound("School year not found.");

            sy.StartYear = dto.StartYear;
            sy.EndYear = dto.EndYear;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var sy = await _context.SchoolYears.FindAsync(id);
            if (sy == null) return NotFound("Not found.");

            _context.SchoolYears.Remove(sy);
            await _context.SaveChangesAsync();

            return Ok(new { message = "School year deleted." });
        }

        [HttpPatch("{id}/set-current")]
        public async Task<IActionResult> SetCurrent(int id)
        {
            var current = await _context.SchoolYears.FirstOrDefaultAsync(y => y.IsCurrent);
            var target = await _context.SchoolYears.FindAsync(id);

            if (target == null)
                return NotFound("School year not found.");

            if (current != null && current.Id != target.Id)
                current.IsCurrent = false;

            target.IsCurrent = true;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"School year {target.StartYear}-{target.EndYear} is now current." });
        }
    }
}
