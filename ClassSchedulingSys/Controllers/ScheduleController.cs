// ClassSchedulingSys/Controllers/ScheduleController
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
    public class ScheduleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
            => _context = context;

        // GET: api/schedule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetAllSchedules()
        {
            var entities = await _context.Schedules
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                .ToListAsync();

            var dtos = entities.Select(MapToReadDto);
            return Ok(dtos);
        }

        // GET: api/schedule/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ScheduleReadDto>> GetScheduleById(int id)
        {
            var entity = await _context.Schedules
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (entity == null)
                return NotFound();

            return Ok(MapToReadDto(entity));
        }

        // In ScheduleController
        [HttpGet("available-rooms")]
        public async Task<ActionResult<IEnumerable<RoomReadDto>>> GetAvailableRooms(
            [FromQuery] DayOfWeek day,
            [FromQuery] TimeSpan startTime,
            [FromQuery] TimeSpan endTime)
        {
            // all rooms
            var allRooms = await _context.Rooms.ToListAsync();

            // rooms booked at that slot
            var booked = await _context.Schedules
                .Where(s => s.Day == day &&
                            ((startTime < s.EndTime && endTime > s.StartTime)))
                .Select(s => s.RoomId)
                .Distinct()
                .ToListAsync();

            var freeRooms = allRooms
                .Where(r => !booked.Contains(r.Id))
                .Select(r => new RoomReadDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Capacity = r.Capacity,
                    Type = r.Type,
                    BuildingId = r.BuildingId,
                    BuildingName = r.Building!.Name
                });

            return Ok(freeRooms);
        }


        // GET: api/schedule/faculty/{facultyId}
        [HttpGet("faculty/{facultyId}")]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetByFaculty(string facultyId)
        {
            if (string.IsNullOrWhiteSpace(facultyId))
                return BadRequest("Faculty ID is required.");

            var entities = await _context.Schedules
                .Where(s => s.FacultyId == facultyId)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                .ToListAsync();

            var dtos = entities.Select(MapToReadDto);
            return Ok(dtos);
        }

        // GET: api/schedule/classsection/{sectionId}
        [HttpGet("classsection/{sectionId:int}")]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetByClassSection(int sectionId)
        {
            var entities = await _context.Schedules
                .Where(s => s.ClassSectionId == sectionId)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                .ToListAsync();

            var dtos = entities.Select(MapToReadDto);
            return Ok(dtos);
        }

        // GET: api/schedule/room/{roomId}
        [HttpGet("room/{roomId:int}")]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetByRoom(int roomId)
        {
            var entities = await _context.Schedules
                .Where(s => s.RoomId == roomId)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                .ToListAsync();

            var dtos = entities.Select(MapToReadDto);
            return Ok(dtos);
        }

        // POST: api/schedule
        [HttpPost]
        public async Task<ActionResult<ScheduleReadDto>> CreateSchedule([FromBody] ScheduleCreateDto dto)
        {
            // Validate assignment
            var assigned = await _context.FacultySubjectAssignments.AnyAsync(a =>
                a.FacultyId == dto.FacultyId &&
                a.SubjectId == dto.SubjectId &&
                a.ClassSectionId == dto.ClassSectionId);

            if (!assigned)
                return BadRequest("Faculty is not assigned to that subject and section.");

            var entity = new Schedule
            {
                Day = dto.Day,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                FacultyId = dto.FacultyId,
                RoomId = dto.RoomId,
                SubjectId = dto.SubjectId,
                ClassSectionId = dto.ClassSectionId,
                IsActive = dto.IsActive
            };

            _context.Schedules.Add(entity);
            await _context.SaveChangesAsync();

            // Reload with navigation props
            var created = await _context.Schedules
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                .FirstOrDefaultAsync(s => s.Id == entity.Id);

            return CreatedAtAction(
                nameof(GetScheduleById),
                new { id = created!.Id },
                MapToReadDto(created));
        }

        // PUT: api/schedule/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateSchedule(int id, [FromBody] ScheduleUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Mismatched schedule ID.");

            var entity = await _context.Schedules.FindAsync(id);
            if (entity == null)
                return NotFound();

            // Validate assignment
            var assigned = await _context.FacultySubjectAssignments.AnyAsync(a =>
                a.FacultyId == dto.FacultyId &&
                a.SubjectId == dto.SubjectId &&
                a.ClassSectionId == dto.ClassSectionId);

            if (!assigned)
                return BadRequest("Faculty is not assigned to that subject and section.");

            // Apply updates
            entity.Day = dto.Day;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;
            entity.FacultyId = dto.FacultyId;
            entity.RoomId = dto.RoomId;
            entity.SubjectId = dto.SubjectId;
            entity.ClassSectionId = dto.ClassSectionId;
            entity.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/schedule/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteSchedule(int id)
        {
            var entity = await _context.Schedules.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.Schedules.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/schedule/check-conflict
        [HttpPost("check-conflict")]
        public async Task<ActionResult<ConflictCheckResultDto>> CheckConflict([FromBody] ScheduleCreateDto dto)
        {
            var conflicts = await _context.Schedules
                .Where(s =>
                    s.Day == dto.Day &&
                    ((dto.StartTime >= s.StartTime && dto.StartTime < s.EndTime) ||
                     (dto.EndTime > s.StartTime && dto.EndTime <= s.EndTime)) &&
                    (s.FacultyId == dto.FacultyId ||
                     s.RoomId == dto.RoomId ||
                     s.ClassSectionId == dto.ClassSectionId))
                .ToListAsync();

            var result = new ConflictCheckResultDto
            {
                HasConflict = conflicts.Any(),
                ConflictingResources = conflicts
                    .Select(c => c.FacultyId == dto.FacultyId
                        ? "Faculty"
                        : c.RoomId == dto.RoomId
                            ? "Room"
                            : "ClassSection")
                    .Distinct()
                    .ToList()
            };

            return Ok(result);
        }

        private class TimeSlot
        {
            public DayOfWeek Day { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
        }

        private TimeSlot? FindAvailableTimeSlot(List<Schedule> existing, FacultySubjectAssignment assignment)
        {
            var duration = TimeSpan.FromHours(assignment.Subject.Units == 0 ? 1 : assignment.Subject.Units); // fallback: 1 hour
            var days = Enum.GetValues<DayOfWeek>();

            foreach (var day in days)
            {
                for (var hour = 8; hour <= 17 - duration.TotalHours; hour++) // limit to 8AM–5PM
                {
                    var start = TimeSpan.FromHours(hour);
                    var end = start + duration;

                    var conflict = existing.Any(s =>
                        s.Day == day &&
                        ((start >= s.StartTime && start < s.EndTime) ||
                         (end > s.StartTime && end <= s.EndTime)) &&
                        (s.FacultyId == assignment.FacultyId ||
                         s.ClassSectionId == assignment.ClassSectionId));

                    if (!conflict)
                    {
                        return new TimeSlot
                        {
                            Day = day,
                            StartTime = start,
                            EndTime = end
                        };
                    }
                }
            }
            return null;
        }

        private static ScheduleReadDto MapToReadDto(Schedule s) => new()
        {
            Id = s.Id,
            Day = s.Day,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Duration = (s.EndTime - s.StartTime).TotalHours,
            FacultyId = s.FacultyId,
            FacultyName = s.Faculty.FullName,
            RoomId = s.RoomId,
            RoomName = s.Room.Name,
            SubjectId = s.SubjectId,
            SubjectTitle = s.Subject.SubjectTitle,
            SubjectUnits = s.Subject.Units,
            SubjectColor = s.Subject.Color ?? "#999999",
            ClassSectionId = s.ClassSectionId,
            ClassSectionName = s.ClassSection.Section,
            IsActive = s.IsActive
        };
    }
}
