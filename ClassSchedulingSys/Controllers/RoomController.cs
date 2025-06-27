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
    public class RoomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/room
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Building)
                .ToListAsync();

            var result = rooms.Select(r => new RoomReadDto
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                Type = r.Type,
                BuildingId = r.BuildingId,
                BuildingName = r.Building?.Name
            });

            return Ok(result);
        }

        // GET: api/room/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Building)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound();

            var dto = new RoomReadDto
            {
                Id = room.Id,
                Name = room.Name,
                Capacity = room.Capacity,
                Type = room.Type,
                BuildingId = room.BuildingId,
                BuildingName = room.Building?.Name
            };

            return Ok(dto);
        }

        // POST: api/room
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoomCreateDto dto)
        {
            var room = new Room
            {
                Name = dto.Name,
                Capacity = dto.Capacity,
                Type = dto.Type,
                BuildingId = dto.BuildingId
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Room created successfully." });
        }

        // PUT: api/room/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RoomUpdateDto dto)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return NotFound();

            room.Name = dto.Name;
            room.Capacity = dto.Capacity;
            room.Type = dto.Type;
            room.BuildingId = dto.BuildingId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Room updated successfully." });
        }

        // DELETE: api/room/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Schedules)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound();

            if (room.Schedules != null && room.Schedules.Any())
                return BadRequest("Cannot delete a room with associated schedules.");

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Room deleted successfully." });
        }

    }
}
