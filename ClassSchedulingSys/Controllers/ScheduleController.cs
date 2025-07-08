using ClassSchedulingSys.Data;
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using ClassSchedulingSys.Services;
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
        private readonly ISchedulePdfService _pdfService;

        public ScheduleController(ApplicationDbContext context, ISchedulePdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // GET: api/schedule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetAllSchedules()
        {
            var entities = await _context.Schedules
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(sem => sem.SchoolYear)
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
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(sem => sem.SchoolYear)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (entity == null)
                return NotFound();

            return Ok(MapToReadDto(entity));
        }

        // GET: api/schedule/faculty/{facultyId}
        [HttpGet("faculty/{facultyId}")]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetByFaculty(string facultyId)
        {
            var entities = await _context.Schedules
                .Where(s => s.FacultyId == facultyId)
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(sem => sem.SchoolYear)
                .ToListAsync();

            return Ok(entities.Select(MapToReadDto));
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
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(sem => sem.SchoolYear)
                .ToListAsync();

            return Ok(entities.Select(MapToReadDto));
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
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(sem => sem.SchoolYear)
                .ToListAsync();

            return Ok(entities.Select(MapToReadDto));
        }

        // POST: api/schedule
        [HttpPost]
        public async Task<ActionResult<ScheduleReadDto>> CreateSchedule([FromBody] ScheduleCreateDto dto)
        {
            var isAssigned = await _context.FacultySubjectAssignments.AnyAsync(a =>
                a.FacultyId == dto.FacultyId &&
                a.SubjectId == dto.SubjectId &&
                a.ClassSectionId == dto.ClassSectionId);

            if (!isAssigned)
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

            var created = await _context.Schedules
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(sem => sem.SchoolYear)
                .FirstOrDefaultAsync(s => s.Id == entity.Id);

            return CreatedAtAction(nameof(GetScheduleById), new { id = created!.Id }, MapToReadDto(created));
        }

        // PUT: api/schedule/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Mismatched schedule ID.");

            var entity = await _context.Schedules.FindAsync(id);
            if (entity == null)
                return NotFound();

            var isAssigned = await _context.FacultySubjectAssignments.AnyAsync(a =>
                a.FacultyId == dto.FacultyId &&
                a.SubjectId == dto.SubjectId &&
                a.ClassSectionId == dto.ClassSectionId);

            if (!isAssigned)
                return BadRequest("Faculty is not assigned to that subject and section.");

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
        public async Task<IActionResult> DeleteSchedule(int id)
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
        public async Task<ActionResult<ConflictCheckResultDto>> CheckConflict(
            [FromBody] ScheduleCreateDto dto,
            [FromQuery] int? scheduleId = null)
        {
            var conflicts = await _context.Schedules
                .Where(s =>
                    s.Day == dto.Day &&
                    ((dto.StartTime >= s.StartTime && dto.StartTime < s.EndTime) ||
                     (dto.EndTime > s.StartTime && dto.EndTime <= s.EndTime)) &&
                    (s.FacultyId == dto.FacultyId ||
                     s.RoomId == dto.RoomId ||
                     s.ClassSectionId == dto.ClassSectionId) &&
                    (!scheduleId.HasValue || s.Id != scheduleId.Value))
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

        // GET: api/schedule/available-rooms
        [HttpGet("available-rooms")]
        public async Task<ActionResult<IEnumerable<RoomReadDto>>> GetAvailableRooms(
            [FromQuery] DayOfWeek day,
            [FromQuery] TimeSpan startTime,
            [FromQuery] TimeSpan endTime)
        {
            var allRooms = await _context.Rooms.Include(r => r.Building).ToListAsync();

            var bookedRoomIds = await _context.Schedules
                .Where(s => s.Day == day &&
                            (startTime < s.EndTime && endTime > s.StartTime))
                .Select(s => s.RoomId)
                .Distinct()
                .ToListAsync();

            var availableRooms = allRooms
                .Where(r => !bookedRoomIds.Contains(r.Id))
                .Select(r => new RoomReadDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Capacity = r.Capacity,
                    Type = r.Type,
                    BuildingId = r.BuildingId,
                    BuildingName = r.Building?.Name ?? "N/A"
                });

            return Ok(availableRooms);
        }

        // GET: api/schedule/print?pov=Faculty&id=abc123
        [HttpGet("print")]
        public async Task<IActionResult> PrintSchedule([FromQuery] string pov, [FromQuery] string id)
        {
            if (string.IsNullOrWhiteSpace(pov) || string.IsNullOrWhiteSpace(id))
                return BadRequest("POV and ID are required.");

            List<Schedule> schedules = pov.ToLower() switch
            {
                "faculty" => await _context.Schedules
                    .Where(s => s.FacultyId == id)
                    .IncludeAll()
                    .ToListAsync(),

                "classsection" => await _context.Schedules
                    .Where(s => s.ClassSectionId == int.Parse(id))
                    .IncludeAll()
                    .ToListAsync(),

                "room" => await _context.Schedules
                    .Where(s => s.RoomId == int.Parse(id))
                    .IncludeAll()
                    .ToListAsync(),

                _ => null!
            };

            if (schedules == null)
                return BadRequest("Invalid POV.");
            if (!schedules.Any())
                return NotFound("No schedules found.");

            var pdfBytes = _pdfService.GenerateSchedulePdf(schedules, pov, id);
            return File(pdfBytes, "application/pdf", $"Schedule_{pov}_{id}.pdf");
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
            SubjectCode = s.Subject.SubjectCode,
            SubjectUnits = s.Subject.Units,
            SubjectColor = s.Subject.Color ?? "#999999",

            ClassSectionId = s.ClassSectionId,
            ClassSectionName = s.ClassSection.Section,
            CourseCode = s.ClassSection.CollegeCourse.Code,
            YearLevel = s.ClassSection.YearLevel.ToString(),

            SemesterName = s.ClassSection.Semester.Name,
            SchoolYearLabel = $"{s.ClassSection.Semester.SchoolYear.StartYear}-{s.ClassSection.Semester.SchoolYear.EndYear}",

            IsActive = s.IsActive
        };
    }

    internal static class ScheduleIncludesExtension
    {
        public static IQueryable<Schedule> IncludeAll(this IQueryable<Schedule> query)
        {
            return query
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(sem => sem.SchoolYear);
        }
    }
}
