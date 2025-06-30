using ClassSchedulingSys.Data;
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Dean,SuperAdmin")]
    public class ScheduleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(ApplicationDbContext context, ILogger<ScheduleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Schedule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetAll()
        {
            var schedules = await _context.Schedules
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                .ToListAsync(); // Move ToListAsync here so we fetch all records first

            var result = schedules.Select(s => new ScheduleReadDto
            {
                Id = s.Id,
                Day = s.Day,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Duration = (s.EndTime - s.StartTime).TotalHours,
                FacultyId = s.FacultyId,
                FacultyName = string.Join(" ", new[] { s.Faculty.FirstName, s.Faculty.MiddleName, s.Faculty.LastName }
                    .Where(n => !string.IsNullOrWhiteSpace(n))),
                RoomId = s.RoomId,
                RoomName = s.Room.Name,
                SubjectId = s.SubjectId,
                SubjectTitle = s.Subject.SubjectTitle,
                SubjectUnits = s.Subject.Units,
                SubjectColor = s.Subject.Color ?? "#999999",
                ClassSectionId = s.ClassSectionId,
                ClassSectionName = s.ClassSection.Section,
                IsActive = s.IsActive
            }).ToList();

            return Ok(result);
        }

        // GET: api/Schedule/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduleReadDto>> GetById(int id)
        {
            var s = await _context.Schedules
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (s == null) return NotFound();

            var dto = new ScheduleReadDto
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

            return Ok(dto);
        }

        // POST: api/Schedule
        [HttpPost]
        public async Task<ActionResult> Create(ScheduleCreateDto dto)
        {
            if (dto.StartTime >= dto.EndTime)
                return BadRequest("Start time must be before end time.");

            var conflictMessage = await CheckForConflicts(dto, null);
            if (conflictMessage != null)
                return Conflict(conflictMessage);

            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            if (subject == null)
                return NotFound("Subject not found.");

            var totalWeeklyHours = await _context.Schedules
                .Where(s =>
                    s.SubjectId == dto.SubjectId &&
                    s.ClassSectionId == dto.ClassSectionId &&
                    s.IsActive)
                .SumAsync(s => EF.Functions.DateDiffMinute(s.StartTime, s.EndTime)) / 60.0;

            var incomingDuration = (dto.EndTime - dto.StartTime).TotalHours;
            if (totalWeeklyHours + incomingDuration > subject.Units)
                return BadRequest("This schedule exceeds the total allowed weekly hours for the subject.");

            var schedule = new Schedule
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

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // PUT: api/Schedule/5
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, ScheduleUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID mismatch.");

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
                return NotFound();

            if (dto.StartTime >= dto.EndTime)
                return BadRequest("Start time must be before end time.");

            var conflictMessage = await CheckForConflicts(new ScheduleCreateDto
            {
                Day = dto.Day,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                FacultyId = dto.FacultyId,
                RoomId = dto.RoomId,
                SubjectId = dto.SubjectId,
                ClassSectionId = dto.ClassSectionId,
                IsActive = dto.IsActive
            }, id);

            if (conflictMessage != null)
                return Conflict(conflictMessage);

            var subject = await _context.Subjects.FindAsync(dto.SubjectId);
            if (subject == null)
                return NotFound("Subject not found.");

            var totalWeeklyHours = await _context.Schedules
                .Where(s =>
                    s.SubjectId == dto.SubjectId &&
                    s.ClassSectionId == dto.ClassSectionId &&
                    s.Id != id &&
                    s.IsActive)
                .SumAsync(s => EF.Functions.DateDiffMinute(s.StartTime, s.EndTime)) / 60.0;

            var incomingDuration = (dto.EndTime - dto.StartTime).TotalHours;
            if (totalWeeklyHours + incomingDuration > subject.Units)
                return BadRequest("This schedule exceeds the total allowed weekly hours for the subject.");

            // Apply updates
            schedule.Day = dto.Day;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;
            schedule.FacultyId = dto.FacultyId;
            schedule.RoomId = dto.RoomId;
            schedule.SubjectId = dto.SubjectId;
            schedule.ClassSectionId = dto.ClassSectionId;
            schedule.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Schedule/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Schedule deleted successfully." });
        }

        // GET: api/Schedule/Faculty/{facultyId}
        [HttpGet("Faculty/{facultyId}")]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetByFaculty(string facultyId)
        {
            var schedules = await _context.Schedules
                .Where(s => s.FacultyId == facultyId)
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.ClassSection)
                .Include(s => s.Faculty)
                .Select(s => new ScheduleReadDto
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
                }).ToListAsync();

            return Ok(schedules);
        }

        // GET: api/Schedule/Room/{roomId}
        [HttpGet("Room/{roomId}")]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetByRoom(int roomId)
        {
            var schedules = await _context.Schedules
                .Where(s => s.RoomId == roomId)
                .Include(s => s.Subject)
                .Include(s => s.Faculty)
                .Include(s => s.ClassSection)
                .Select(s => new ScheduleReadDto
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
                }).ToListAsync();

            return Ok(schedules);
        }

        // GET: api/Schedule/ClassSection/{classSectionId}
        [HttpGet("ClassSection/{classSectionId}")]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetByClassSection(int classSectionId)
        {
            var schedules = await _context.Schedules
                .Where(s => s.ClassSectionId == classSectionId)
                .Include(s => s.Subject)
                .Include(s => s.Room)
                .Include(s => s.Faculty)
                .Select(s => new ScheduleReadDto
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
                })
                .ToListAsync();

            return Ok(schedules);
        }


        // CONFLICT CHECKER
        private async Task<string?> CheckForConflicts(ScheduleCreateDto dto, int? ignoreId)
        {
            var baseQuery = _context.Schedules
                .Where(s => s.Day == dto.Day && s.Id != ignoreId && s.IsActive);

            // Faculty conflict
            bool facultyConflict = await baseQuery.AnyAsync(s =>
                s.FacultyId == dto.FacultyId &&
                (
                    (dto.StartTime >= s.StartTime && dto.StartTime < s.EndTime) ||
                    (dto.EndTime > s.StartTime && dto.EndTime <= s.EndTime) ||
                    (dto.StartTime <= s.StartTime && dto.EndTime >= s.EndTime)
                ));

            if (facultyConflict)
                return "Conflict: Faculty is already assigned to another class at this time.";

            // Room conflict
            bool roomConflict = await baseQuery.AnyAsync(s =>
                s.RoomId == dto.RoomId &&
                (
                    (dto.StartTime >= s.StartTime && dto.StartTime < s.EndTime) ||
                    (dto.EndTime > s.StartTime && dto.EndTime <= s.EndTime) ||
                    (dto.StartTime <= s.StartTime && dto.EndTime >= s.EndTime)
                ));

            if (roomConflict)
                return "Conflict: Room is already booked at this time.";

            // Section conflict
            bool sectionConflict = await baseQuery.AnyAsync(s =>
                s.ClassSectionId == dto.ClassSectionId &&
                (
                    (dto.StartTime >= s.StartTime && dto.StartTime < s.EndTime) ||
                    (dto.EndTime > s.StartTime && dto.EndTime <= s.EndTime) ||
                    (dto.StartTime <= s.StartTime && dto.EndTime >= s.EndTime)
                ));

            if (sectionConflict)
                return "Conflict: Class section already has a schedule at this time.";

            return null;
        }
    }
}
