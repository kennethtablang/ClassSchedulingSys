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
    [Authorize(Roles = "Dean,SuperAdmin")]
    public class SemesterController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SemesterController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SemesterDto>>> GetAll()
        {
            var semesters = await _context.Semesters
                .Include(s => s.SchoolYear)
                .Select(s => new SemesterDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    SchoolYearId = s.SchoolYearId,
                    IsCurrent = s.IsCurrent,
                    IsSchoolYearCurrent = s.SchoolYear != null && s.SchoolYear.IsCurrent,
                    SchoolYearLabel = s.SchoolYear != null
                        ? $"{s.SchoolYear.StartYear}-{s.SchoolYear.EndYear}"
                        : null
                })
                .ToListAsync();

            return Ok(semesters);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SemesterDto>> GetById(int id)
        {
            var semester = await _context.Semesters
                .Include(s => s.SchoolYear)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (semester == null) return NotFound();

            var dto = new SemesterDto
            {
                Id = semester.Id,
                Name = semester.Name,
                StartDate = semester.StartDate,
                EndDate = semester.EndDate,
                SchoolYearId = semester.SchoolYearId,
                IsCurrent = semester.IsCurrent,
                IsSchoolYearCurrent = semester.SchoolYear != null && semester.SchoolYear.IsCurrent,
                SchoolYearLabel = semester.SchoolYear != null
                    ? $"{semester.SchoolYear.StartYear}-{semester.SchoolYear.EndYear}"
                    : null
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSemesterDto dto)
        {
            var semester = new Semester
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                SchoolYearId = dto.SchoolYearId,
                IsCurrent = false
            };

            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Semester created successfully." });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateSemesterDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch.");

            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();

            semester.Name = dto.Name;
            semester.StartDate = dto.StartDate;
            semester.EndDate = dto.EndDate;
            semester.SchoolYearId = dto.SchoolYearId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Semester updated successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();

            if (semester.IsCurrent)
                return BadRequest("Cannot delete the current semester.");

            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Semester deleted." });
        }

        [HttpPatch("{id}/set-current")]
        public async Task<IActionResult> SetAsCurrent(int id)
        {
            var semester = await _context.Semesters
                .Include(s => s.SchoolYear)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (semester == null) return NotFound();

            if (semester.SchoolYear == null || !semester.SchoolYear.IsCurrent)
                return BadRequest("Cannot set semester as current because its school year is not active.");

            var allSemesters = await _context.Semesters.ToListAsync();
            foreach (var sem in allSemesters)
            {
                sem.IsCurrent = false;
            }

            semester.IsCurrent = true;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Semester set as current." });
        }
    }
}
