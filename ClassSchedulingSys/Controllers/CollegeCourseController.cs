// ClassSchedulingSys/Controllers/CollegeCourseController.cs
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
    public class CollegeCourseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CollegeCourseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/collegecourse
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CollegeCourseDto>>> GetAll()
        {
            var courses = await _context.CollegeCourses
                .Select(c => new CollegeCourseDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET: api/collegecourse/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CollegeCourseDto>> GetById(int id)
        {
            var course = await _context.CollegeCourses.FindAsync(id);
            if (course == null) return NotFound();

            var dto = new CollegeCourseDto
            {
                Id = course.Id,
                Code = course.Code,
                Name = course.Name
            };

            return Ok(dto);
        }

        // POST: api/collegecourse
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCollegeCourseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Normalize inputs
            var code = dto.Code?.Trim();
            var name = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Course code is required.");

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Course name is required.");

            // Case-insensitive check for existing code
            var exists = await _context.CollegeCourses
                .AnyAsync(c => c.Code != null && c.Code.ToUpper() == code.ToUpper());

            if (exists)
                return BadRequest("Course code already taken.");

            var course = new CollegeCourse
            {
                Code = code,
                Name = name
            };

            _context.CollegeCourses.Add(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "College course added successfully.", course.Id });
        }

        // PUT: api/collegecourse/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCollegeCourseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.Id)
                return BadRequest("ID mismatch.");

            // Normalize incoming values
            var newCode = dto.Code?.Trim();
            var newName = dto.Name?.Trim();

            if (string.IsNullOrWhiteSpace(newCode))
                return BadRequest("Course code is required.");

            if (string.IsNullOrWhiteSpace(newName))
                return BadRequest("Course name is required.");

            var course = await _context.CollegeCourses.FindAsync(id);
            if (course == null) return NotFound();

            // Ensure no other course (different id) uses that code (case-insensitive)
            var codeTaken = await _context.CollegeCourses
                .AnyAsync(c => c.Id != id && c.Code != null && c.Code.ToUpper() == newCode.ToUpper());

            if (codeTaken)
                return BadRequest("Course code already taken.");

            // Apply updates
            course.Code = newCode;
            course.Name = newName;

            await _context.SaveChangesAsync();
            return Ok(new { message = "College course updated." });
        }

        // DELETE: api/collegecourse/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.CollegeCourses.FindAsync(id);
            if (course == null) return NotFound();

            _context.CollegeCourses.Remove(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "College course deleted." });
        }
    }
}
