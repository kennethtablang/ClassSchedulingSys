using ClassSchedulingSys.Data;
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

        // GET: api/semester
        [HttpGet]
        public async Task<IActionResult> GetSemesters()
        {
            var semesters = await _context.Semesters.Include(s => s.SchoolYear).ToListAsync();
            return Ok(semesters);
        }

        // GET: api/semester/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSemester(int id)
        {
            var semester = await _context.Semesters.Include(s => s.SchoolYear).FirstOrDefaultAsync(s => s.Id == id);
            if (semester == null) return NotFound();
            return Ok(semester);
        }

        // POST: api/semester
        [HttpPost]
        public async Task<IActionResult> CreateSemester([FromBody] Semester semester)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Semesters.Add(semester);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSemester), new { id = semester.Id }, semester);
        }

        // PUT: api/semester/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSemester(int id, [FromBody] Semester updatedSemester)
        {
            if (id != updatedSemester.Id) return BadRequest("ID mismatch");

            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();

            semester.Name = updatedSemester.Name;
            semester.SchoolYearId = updatedSemester.SchoolYearId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/semester/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSemester(int id)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null) return NotFound();

            _context.Semesters.Remove(semester);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Semester {id} deleted." });
        }
    }
}
