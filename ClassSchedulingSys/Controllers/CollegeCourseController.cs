// ClassSchedulingSys/Controllers/CollegeCourseController
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
        public async Task<IActionResult> Create(CreateCollegeCourseDto dto)
        {
            var course = new CollegeCourse
            {
                Code = dto.Code,
                Name = dto.Name
            };

            _context.CollegeCourses.Add(course);
            await _context.SaveChangesAsync();

            return Ok(new { message = "College course added successfully." });
        }

        // PUT: api/collegecourse/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateCollegeCourseDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch.");

            var course = await _context.CollegeCourses.FindAsync(id);
            if (course == null) return NotFound();

            course.Code = dto.Code;
            course.Name = dto.Name;

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
