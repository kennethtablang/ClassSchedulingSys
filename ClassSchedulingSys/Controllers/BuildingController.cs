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
    public class BuildingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BuildingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/building
        [HttpGet]
        public async Task<IActionResult> GetBuildings()
        {
            var buildings = await _context.Buildings.Include(b => b.Rooms).ToListAsync();
            return Ok(buildings);
        }

        // GET: api/building/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBuilding(int id)
        {
            var building = await _context.Buildings.Include(b => b.Rooms).FirstOrDefaultAsync(b => b.Id == id);
            if (building == null)
                return NotFound();

            return Ok(building);
        }

        // POST: api/building
        [HttpPost]
        public async Task<IActionResult> CreateBuilding([FromBody] Building building)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, building);
        }

        // PUT: api/building/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBuilding(int id, [FromBody] Building updatedBuilding)
        {
            if (id != updatedBuilding.Id)
                return BadRequest("ID mismatch");

            var building = await _context.Buildings.FindAsync(id);
            if (building == null)
                return NotFound();

            building.Name = updatedBuilding.Name;
            building.Description = updatedBuilding.Description;

            _context.Buildings.Update(building);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/building/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBuilding(int id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null)
                return NotFound();

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Building {id} deleted successfully." });
        }
    }
}
