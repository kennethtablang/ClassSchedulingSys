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
    public class SubjectController : Controller
    {
        public readonly ApplicationDbContext _context;

        //constructor
        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        //GET: api/subject
        [HttpGet]
        public async Task<IActionResult> GetSubjects()
        {
            var subjects = await _context.Subjects.ToListAsync();
            return Ok(subjects);
        }

        //GET: api/subject/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
            {
                return NotFound();
            }
            return Ok(subject);
        }

        //POST: api/subject
        [HttpPost]
        public async Task<IActionResult> CreateSubject([FromBody] Subject subject)
        {
            if (subject == null || string.IsNullOrWhiteSpace(subject.Code) || string.IsNullOrWhiteSpace(subject.Title))
            {
                return BadRequest("Invalid subject data.");
            }
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subject);
        }

        // PUT: api/subject/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubject(int id, [FromBody] Subject subject)
        {
            if (id != subject.Id)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _context.Subjects.AnyAsync(c => c.Id == id);
            if (!exists)
                return NotFound();

            _context.Entry(subject).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/subject/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound();

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Subject {id} deleted successfully." });
        }
    }
}
