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
    public class RoomController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/room
        [HttpGet]
        public async Task<IActionResult> GetRooms()
        {
            var rooms = await _context.Rooms.Include(r => r.Building).ToListAsync();
            return Ok(rooms);
        }

        // GET: api/room/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoom(int id)
        {
            var room = await _context.Rooms.Include(r => r.Building).FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
                return NotFound();

            return Ok(room);
        }

        // POST: api/room
        [HttpPost]
        public async Task<IActionResult> CreateRoom([FromBody] Room room)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }

        // PUT: api/room/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, [FromBody] Room updatedRoom)
        {
            if (id != updatedRoom.Id)
                return BadRequest("ID mismatch");

            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return NotFound();

            room.Name = updatedRoom.Name;
            room.Capacity = updatedRoom.Capacity;
            room.Type = updatedRoom.Type;
            room.BuildingId = updatedRoom.BuildingId;

            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/room/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return NotFound();

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"Room {id} deleted successfully." });
        }
    }
}
