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
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/schedule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetAll()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.ClassSection)
                .ToListAsync();

            var result = schedules.Select(s => new ScheduleReadDto
            {
                Id = s.Id,
                Day = s.Day,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                RoomId = s.RoomId,
                RoomName = s.Room?.Name ?? "",
                FacultyId = s.FacultyId,
                FacultyName = $"{s.Faculty?.FirstName} {s.Faculty?.LastName}",
                SubjectId = s.SubjectId,
                SubjectTitle = s.Subject?.SubjectTitle ?? "",
                SubjectColor = s.Subject?.Color ?? "#000000",
                SubjectUnits = s.Subject?.Units ?? 0,
                ClassSectionId = s.ClassSectionId,
                ClassSectionName = s.ClassSection?.Section ?? ""
            }).ToList();

            return Ok(result);
        }

        // GET: api/schedule/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduleReadDto>> GetById(int id)
        {
            var s = await _context.Schedules
                .Include(x => x.Room)
                .Include(x => x.Subject)
                .Include(x => x.Faculty)
                .Include(x => x.ClassSection)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s == null) return NotFound();

            var dto = new ScheduleReadDto
            {
                Id = s.Id,
                Day = s.Day,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                RoomId = s.RoomId,
                RoomName = s.Room?.Name ?? "",
                FacultyId = s.FacultyId,
                FacultyName = $"{s.Faculty?.FirstName} {s.Faculty?.LastName}",
                SubjectId = s.SubjectId,
                SubjectTitle = s.Subject?.SubjectTitle ?? "",
                SubjectColor = s.Subject?.Color ?? "#000000",
                SubjectUnits = s.Subject?.Units ?? 0,
                ClassSectionId = s.ClassSectionId,
                ClassSectionName = s.ClassSection?.Section ?? ""
            };

            return Ok(dto);
        }

        // POST: api/schedule
        [HttpPost]
        public async Task<ActionResult> Create(ScheduleCreateDto dto)
        {
            var schedule = new Schedule
            {
                Day = dto.Day,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                RoomId = dto.RoomId,
                FacultyId = dto.FacultyId,
                SubjectId = dto.SubjectId,
                ClassSectionId = dto.ClassSectionId
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Schedule created successfully." });
        }

        // PUT: api/schedule/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ScheduleUpdateDto dto)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            schedule.Day = dto.Day;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.RoomId = dto.RoomId;
            schedule.FacultyId = dto.FacultyId;
            schedule.SubjectId = dto.SubjectId;
            schedule.ClassSectionId = dto.ClassSectionId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Schedule updated successfully." });
        }

        // DELETE: api/schedule/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Schedule deleted successfully." });
        }

        // GET: api/schedule/by-section/{sectionId}
        [HttpGet("by-section/{sectionId}")]
        public async Task<IActionResult> GetBySection(int sectionId)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.ClassSection)
                .Include(s => s.Faculty)
                .Where(s => s.ClassSectionId == sectionId)
                .ToListAsync();

            var result = schedules.Select(s => new ScheduleReadDto
            {
                Id = s.Id,
                Day = s.Day,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                SubjectId = s.SubjectId,
                SubjectTitle = s.Subject?.SubjectTitle,
                SubjectColor = s.Subject?.Color,
                FacultyId = s.FacultyId,
                FacultyName = $"{s.Faculty?.FirstName} {s.Faculty?.LastName}",
                RoomId = s.RoomId,
                RoomName = s.Room?.Name,
                ClassSectionId = s.ClassSectionId,
                ClassSectionName = s.ClassSection?.Section
            });

            return Ok(result);
        }

        // GET: api/schedule/by-faculty/{facultyId}
        [HttpGet("by-faculty/{facultyId}")]
        public async Task<IActionResult> GetByFaculty(string facultyId)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.ClassSection)
                .Include(s => s.Faculty)
                .Where(s => s.FacultyId == facultyId)
                .ToListAsync();

            var result = schedules.Select(s => new ScheduleReadDto
            {
                Id = s.Id,
                Day = s.Day,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                SubjectId = s.SubjectId,
                SubjectTitle = s.Subject?.SubjectTitle,
                SubjectColor = s.Subject?.Color,
                FacultyId = s.FacultyId,
                FacultyName = $"{s.Faculty?.FirstName} {s.Faculty?.LastName}",
                RoomId = s.RoomId,
                RoomName = s.Room?.Name,
                ClassSectionId = s.ClassSectionId,
                ClassSectionName = s.ClassSection?.Section
            });

            return Ok(result);
        }

        // GET: api/schedule/by-room/{roomId}
        [HttpGet("by-room/{roomId}")]
        public async Task<IActionResult> GetByRoom(int roomId)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.ClassSection)
                .Include(s => s.Faculty)
                .Where(s => s.RoomId == roomId)
                .ToListAsync();

            var result = schedules.Select(s => new ScheduleReadDto
            {
                Id = s.Id,
                Day = s.Day,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                SubjectId = s.SubjectId,
                SubjectTitle = s.Subject?.SubjectTitle,
                SubjectColor = s.Subject?.Color,
                FacultyId = s.FacultyId,
                FacultyName = $"{s.Faculty?.FirstName} {s.Faculty?.LastName}",
                RoomId = s.RoomId,
                RoomName = s.Room?.Name,
                ClassSectionId = s.ClassSectionId,
                ClassSectionName = s.ClassSection?.Section
            });

            return Ok(result);
        }
    }
}
