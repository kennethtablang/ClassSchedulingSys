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
    public class ClassController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/class
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var classes = await _context.Classes
                .Include(c => c.Department)
                .ToListAsync();
            return Ok(classes);
        }

        // GET: api/class/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Classes
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (item == null) return NotFound();
            return Ok(item);
        }

        // POST: api/class
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Class model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Classes.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        // PUT: api/class/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Class model)
        {
            if (id != model.Id) return BadRequest("ID mismatch");

            var existing = await _context.Classes.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.DepartmentId = model.DepartmentId;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/class/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.Classes.FindAsync(id);
            if (existing == null) return NotFound();

            _context.Classes.Remove(existing);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Class {id} deleted." });
        }

    }
}
