// ClassSchedulingSys/Controllers/BuildingController
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
    [Authorize(Roles = "SuperAdmin")]
    public class BuildingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BuildingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/building
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var buildings = await _context.Buildings
                .Include(b => b.Rooms)
                .ToListAsync();

            var result = buildings.Select(b => new BuildingReadDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                IsActive = b.IsActive,
                Rooms = b.Rooms?.Select(r => new RoomDto
                {
                    Id = r.Id,
                    Name = r.Name
                }).ToList()
            });

            return Ok(result);
        }

        // GET: api/building/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var building = await _context.Buildings
                .Include(b => b.Rooms)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (building == null)
                return NotFound();

            var dto = new BuildingReadDto
            {
                Id = building.Id,
                Name = building.Name,
                Description = building.Description,
                IsActive = building.IsActive,
                Rooms = building.Rooms?.Select(r => new RoomDto
                {
                    Id = r.Id,
                    Name = r.Name
                }).ToList()
            };

            return Ok(dto);
        }

        // POST: api/building
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BuildingCreateDto dto)
        {
            var building = new Building
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = true
            };

            _context.Buildings.Add(building);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Building created successfully." });
        }

        // PUT: api/building/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BuildingUpdateDto dto)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null)
                return NotFound();

            building.Name = dto.Name;
            building.Description = dto.Description;
            building.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Building updated successfully." });
        }

        // DELETE: api/building/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var building = await _context.Buildings
                .Include(b => b.Rooms)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (building == null)
                return NotFound();

            if (building.Rooms != null && building.Rooms.Any())
            {
                return BadRequest("Cannot delete a building with associated rooms.");
            }

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Building deleted successfully." });
        }
    }
}
