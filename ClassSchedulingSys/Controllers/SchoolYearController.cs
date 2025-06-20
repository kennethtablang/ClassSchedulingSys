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
    public class SchoolYearController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SchoolYearController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/schoolyear
        [HttpGet]
        public async Task<IActionResult> GetSchoolYears()
        {
            var schoolYears = await _context.SchoolYears.Include(y => y.Semesters).ToListAsync();
            return Ok(schoolYears);
        }

        // GET: api/schoolyear/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchoolYear(int id)
        {
            var schoolYear = await _context.SchoolYears.Include(y => y.Semesters).FirstOrDefaultAsync(y => y.Id == id);
            if (schoolYear == null) return NotFound();
            return Ok(schoolYear);
        }

        // POST: api/schoolyear
        [HttpPost]
        public async Task<IActionResult> CreateSchoolYear([FromBody] SchoolYear schoolYear)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.SchoolYears.Add(schoolYear);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSchoolYear), new { id = schoolYear.Id }, schoolYear);
        }

        // PUT: api/schoolyear/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchoolYear(int id, [FromBody] SchoolYear updatedYear)
        {
            if (id != updatedYear.Id) return BadRequest("ID mismatch");

            var schoolYear = await _context.SchoolYears.FindAsync(id);
            if (schoolYear == null) return NotFound();

            schoolYear.Year = updatedYear.Year;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/schoolyear/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchoolYear(int id)
        {
            var schoolYear = await _context.SchoolYears.FindAsync(id);
            if (schoolYear == null) return NotFound();

            _context.SchoolYears.Remove(schoolYear);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"School Year {id} deleted." });
        }
    }
}
