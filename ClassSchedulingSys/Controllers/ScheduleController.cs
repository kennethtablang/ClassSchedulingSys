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
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/schedule
        [HttpGet]
        public async Task<IActionResult> GetSchedules()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Semester)
                    .ThenInclude(sem => sem.SchoolYear)
                .Include(s => s.Class)
                .ToListAsync();

            return Ok(schedules);
        }

        // GET: api/schedule/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSchedule(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Semester)
                    .ThenInclude(sem => sem.SchoolYear)
                .Include(s => s.Class)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null) return NotFound();
            return Ok(schedule);
        }

        // GET: api/schedule/faculty/{id}
        [HttpGet("faculty/{id}")]
        public async Task<IActionResult> GetSchedulesByFaculty(string id)
        {
            var schedules = await _context.Schedules
                .Where(s => s.FacultyId == id)
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.Semester)
                    .ThenInclude(sem => sem.SchoolYear)
                .Include(s => s.Class)
                .ToListAsync();

            return Ok(schedules);
        }

        // GET: api/schedule/room/{id}
        [HttpGet("room/{id}")]
        public async Task<IActionResult> GetSchedulesByRoom(int id)
        {
            var schedules = await _context.Schedules
                .Where(s => s.RoomId == id)
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.Semester)
                    .ThenInclude(sem => sem.SchoolYear)
                .Include(s => s.Class)
                .ToListAsync();

            return Ok(schedules);
        }

        // GET: api/schedule/semester/{id}
        [HttpGet("semester/{id}")]
        public async Task<IActionResult> GetSchedulesBySemester(int id)
        {
            var schedules = await _context.Schedules
                .Where(s => s.SemesterId == id)
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Semester)
                    .ThenInclude(sem => sem.SchoolYear)
                .Include(s => s.Class)
                .ToListAsync();

            return Ok(schedules);
        }

        // GET: api/schedule/class/{id}
        [HttpGet("class/{id}")]
        public async Task<IActionResult> GetSchedulesByClass(int id)
        {
            var schedules = await _context.Schedules
                .Where(s => s.ClassId == id)
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Semester)
                    .ThenInclude(sem => sem.SchoolYear)
                .Include(s => s.Class)
                .ToListAsync();

            return Ok(schedules);
        }

        // POST: api/schedule
        [HttpPost]
        public async Task<IActionResult> CreateSchedule([FromBody] Schedule schedule)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (schedule.StartTime >= schedule.EndTime)
                return BadRequest("Start time must be earlier than end time.");

            // Conflict check for faculty
            var facultyConflict = await _context.Schedules.AnyAsync(s =>
                s.FacultyId == schedule.FacultyId &&
                s.Day == schedule.Day &&
                s.StartTime < schedule.EndTime &&
                s.EndTime > schedule.StartTime);

            if (facultyConflict)
                return Conflict("Faculty is already assigned during this time.");

            // Conflict check for room
            var roomConflict = await _context.Schedules.AnyAsync(s =>
                s.RoomId == schedule.RoomId &&
                s.Day == schedule.Day &&
                s.StartTime < schedule.EndTime &&
                s.EndTime > schedule.StartTime);

            if (roomConflict)
                return Conflict("Room is already booked during this time.");

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
        }

        // PUT: api/schedule/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] Schedule updatedSchedule)
        {
            if (id != updatedSchedule.Id) return BadRequest("ID mismatch");

            if (updatedSchedule.StartTime >= updatedSchedule.EndTime)
                return BadRequest("Start time must be earlier than end time.");

            var existing = await _context.Schedules.FindAsync(id);
            if (existing == null) return NotFound();

            // Conflict checks (excluding the current one)
            var conflictFaculty = await _context.Schedules.AnyAsync(s =>
                s.Id != id &&
                s.FacultyId == updatedSchedule.FacultyId &&
                s.Day == updatedSchedule.Day &&
                s.StartTime < updatedSchedule.EndTime &&
                s.EndTime > updatedSchedule.StartTime);

            var conflictRoom = await _context.Schedules.AnyAsync(s =>
                s.Id != id &&
                s.RoomId == updatedSchedule.RoomId &&
                s.Day == updatedSchedule.Day &&
                s.StartTime < updatedSchedule.EndTime &&
                s.EndTime > updatedSchedule.StartTime);

            if (conflictFaculty)
                return Conflict("Faculty has a schedule conflict.");

            if (conflictRoom)
                return Conflict("Room is already booked.");

            existing.SubjectId = updatedSchedule.SubjectId;
            existing.FacultyId = updatedSchedule.FacultyId;
            existing.RoomId = updatedSchedule.RoomId;
            existing.SemesterId = updatedSchedule.SemesterId;
            existing.ClassId = updatedSchedule.ClassId;
            existing.Day = updatedSchedule.Day;
            existing.StartTime = updatedSchedule.StartTime;
            existing.EndTime = updatedSchedule.EndTime;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/schedule/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Schedule {id} deleted." });
        }
    }
}
