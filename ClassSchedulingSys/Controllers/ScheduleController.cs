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
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")]
    public class ScheduleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISchedulePdfService _pdfService;
        private readonly INotificationService _notificationService;

        public ScheduleController(ApplicationDbContext context, ISchedulePdfService pdfService, INotificationService notificationService)
        {
            _context = context;
            _pdfService = pdfService;
            _notificationService = notificationService;
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

        //// POST: api/schedule
        //[HttpPost]
        //public async Task<ActionResult<ScheduleReadDto>> CreateSchedule([FromBody] ScheduleCreateDto dto)
        //{
        //    var isAssigned = await _context.FacultySubjectAssignments.AnyAsync(a =>
        //        a.FacultyId == dto.FacultyId &&
        //        a.SubjectId == dto.SubjectId &&
        //        a.ClassSectionId == dto.ClassSectionId);

        //    if (!isAssigned)
        //        return BadRequest("Faculty is not assigned to that subject and section.");

        //    var entity = new Schedule
        //    {
        //        Day = dto.Day,
        //        StartTime = dto.StartTime,
        //        EndTime = dto.EndTime,
        //        FacultyId = dto.FacultyId,
        //        RoomId = dto.RoomId,
        //        SubjectId = dto.SubjectId,
        //        ClassSectionId = dto.ClassSectionId,
        //        IsActive = dto.IsActive
        //    };

        //    _context.Schedules.Add(entity);
        //    await _context.SaveChangesAsync();

        //    var created = await _context.Schedules
        //        .Include(s => s.Faculty)
        //        .Include(s => s.Room)
        //        .Include(s => s.Subject)
        //        .Include(s => s.ClassSection)
        //            .ThenInclude(cs => cs.CollegeCourse)
        //        .Include(s => s.ClassSection)
        //            .ThenInclude(cs => cs.Semester)
        //                .ThenInclude(sem => sem.SchoolYear)
        //        .FirstOrDefaultAsync(s => s.Id == entity.Id);

        //    return CreatedAtAction(nameof(GetScheduleById), new { id = created!.Id }, MapToReadDto(created));
        //}

        // POST: api/schedule
        [HttpPost]
        public async Task<ActionResult<ScheduleReadDto>> CreateSchedule([FromBody] ScheduleCreateDto dto)
        {
            // Ensure faculty is assigned to the subject/section
            var isAssigned = await _context.FacultySubjectAssignments.AnyAsync(a =>
                a.FacultyId == dto.FacultyId &&
                a.SubjectId == dto.SubjectId &&
                a.ClassSectionId == dto.ClassSectionId);

            if (!isAssigned)
                return BadRequest("Faculty is not assigned to that subject and section.");

            // Load the ClassSection to read its SemesterId (and optionally its Semester navigation)
            var classSection = await _context.ClassSections
                .Include(cs => cs.Semester) // optional, useful for later validation/display
                .FirstOrDefaultAsync(cs => cs.Id == dto.ClassSectionId);

            if (classSection == null)
                return BadRequest("Class section not found.");

            // Prevent creating schedule for a class section without a semester
            if (classSection.SemesterId == 0 || classSection.Semester == null)
                return BadRequest("The selected class section does not have a semester assigned. Please assign a semester to the class section first.");

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

            // === Important: set the DB column Schedules.SemesterId (database-only column) ===
            // Use a raw SQL update to avoid touching the EF model or adding migrations.
            if (classSection.SemesterId != 0)
            {
                // Parameterized using ExecuteSqlInterpolated to avoid injection
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE dbo.Schedules SET SemesterId = {classSection.SemesterId} WHERE Id = {entity.Id}");
            }

            // Reload the schedule with navigation properties for the response
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


        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Mismatched schedule ID.");

            // Load full entity including navigation props
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

            var isAssigned = await _context.FacultySubjectAssignments.AnyAsync(a =>
                a.FacultyId == dto.FacultyId &&
                a.SubjectId == dto.SubjectId &&
                a.ClassSectionId == dto.ClassSectionId);

            if (!isAssigned)
                return BadRequest("Faculty is not assigned to that subject and section.");

            // Load the *target* class section to get its SemesterId
            var targetClassSection = await _context.ClassSections
                .Include(cs => cs.Semester)
                .FirstOrDefaultAsync(cs => cs.Id == dto.ClassSectionId);

            if (targetClassSection == null)
                return BadRequest("Target class section not found.");

            if (targetClassSection.SemesterId == 0 || targetClassSection.Semester == null)
                return BadRequest("The selected class section does not have a semester assigned. Please assign a semester to the class section first.");

            // Capture old notify info (if you need notifications)
            var oldInfo = BuildNotifyInfo(entity);
            var oldFacultyEmail = entity.Faculty?.Email;
            var oldFacultyName = entity.Faculty?.FullName;

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

            // Update DB column Schedules.SemesterId to reflect the ClassSection.SemesterId
            if (targetClassSection.SemesterId != 0)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE dbo.Schedules SET SemesterId = {targetClassSection.SemesterId} WHERE Id = {entity.Id}");
            }

            // Reload updated schedule for notification and response
            var updated = await _context.Schedules
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.Subject)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(s => s.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(sem => sem.SchoolYear)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (updated == null)
                return StatusCode(500, "Schedule updated but failed to load updated data for notifications.");

            var newInfo = BuildNotifyInfo(updated);
            var newFacultyEmail = updated.Faculty?.Email;
            var newFacultyName = updated.Faculty?.FullName;

            await _notificationService.NotifyScheduleUpdatedAsync(oldInfo, newInfo, oldFacultyEmail, oldFacultyName, newFacultyEmail, newFacultyName);

            return NoContent();
        }


        //[HttpPut("{id:int}")]
        //public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleUpdateDto dto)
        //{
        //    if (id != dto.Id)
        //        return BadRequest("Mismatched schedule ID.");

        //    // Load full entity to capture "old" state including navigations
        //    var entity = await _context.Schedules
        //        .Include(s => s.Faculty)
        //        .Include(s => s.Room)
        //        .Include(s => s.Subject)
        //        .Include(s => s.ClassSection)
        //            .ThenInclude(cs => cs.CollegeCourse)
        //        .Include(s => s.ClassSection)
        //            .ThenInclude(cs => cs.Semester)
        //                .ThenInclude(sem => sem.SchoolYear)
        //        .FirstOrDefaultAsync(s => s.Id == id);

        //    if (entity == null)
        //        return NotFound();

        //    var isAssigned = await _context.FacultySubjectAssignments.AnyAsync(a =>
        //        a.FacultyId == dto.FacultyId &&
        //        a.SubjectId == dto.SubjectId &&
        //        a.ClassSectionId == dto.ClassSectionId);

        //    if (!isAssigned)
        //        return BadRequest("Faculty is not assigned to that subject and section.");

        //    // Capture old data
        //    var oldInfo = BuildNotifyInfo(entity);
        //    var oldFacultyEmail = entity.Faculty?.Email;
        //    var oldFacultyName = entity.Faculty?.FullName;

        //    // Apply updates
        //    entity.Day = dto.Day;
        //    entity.StartTime = dto.StartTime;
        //    entity.EndTime = dto.EndTime;
        //    entity.FacultyId = dto.FacultyId;
        //    entity.RoomId = dto.RoomId;
        //    entity.SubjectId = dto.SubjectId;
        //    entity.ClassSectionId = dto.ClassSectionId;
        //    entity.IsActive = dto.IsActive;

        //    await _context.SaveChangesAsync();

        //    // Reload to get updated navigation props
        //    var updated = await _context.Schedules
        //        .Include(s => s.Faculty)
        //        .Include(s => s.Room)
        //        .Include(s => s.Subject)
        //        .Include(s => s.ClassSection)
        //            .ThenInclude(cs => cs.CollegeCourse)
        //        .Include(s => s.ClassSection)
        //            .ThenInclude(cs => cs.Semester)
        //                .ThenInclude(sem => sem.SchoolYear)
        //        .FirstOrDefaultAsync(s => s.Id == id);

        //    if (updated == null)
        //        return StatusCode(500, "Schedule updated but failed to load updated data for notifications.");

        //    var newInfo = BuildNotifyInfo(updated);
        //    var newFacultyEmail = updated.Faculty?.Email;
        //    var newFacultyName = updated.Faculty?.FullName;

        //    // Call the single method that handles all notification cases
        //    await _notificationService.NotifyScheduleUpdatedAsync(oldInfo, newInfo, oldFacultyEmail, oldFacultyName, newFacultyEmail, newFacultyName);

        //    return NoContent();
        //}

        [HttpPost("testing/send-test-email")]
        public async Task<IActionResult> SendTestEmail([FromServices] IBackgroundEmailQueue queue)
        {
            queue.Enqueue(new EmailMessage("kennethreytablang@gmail.com", "Test from queue", "<p>This is a test.</p>"));
            return Ok("Enqueued");
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

        // ✅ FIXED: Improved conflict checking with better error messages
        [HttpPost("check-conflict")]
        public async Task<ActionResult<ConflictCheckResultDto>> CheckConflict(
            [FromBody] ScheduleConflictCheckDto dto)
        {
            // ✅ Parse the day properly (could be string or enum)
            DayOfWeek dayOfWeek;
            if (dto.Day is string dayStr)
            {
                if (!Enum.TryParse<DayOfWeek>(dayStr, true, out dayOfWeek))
                {
                    return BadRequest($"Invalid day value: {dayStr}");
                }
            }
            else
            {
                dayOfWeek = (DayOfWeek)dto.Day;
            }

            // Validate time range
            if (dto.EndTime <= dto.StartTime)
            {
                return BadRequest("End time must be after start time");
            }

            // Query for conflicts with detailed resource tracking
            var conflicts = await _context.Schedules
                .Where(s =>
                    s.IsActive &&
                    s.Day == dayOfWeek &&
                    // Time overlap: (StartA < EndB) AND (EndA > StartB)
                    (dto.StartTime < s.EndTime && dto.EndTime > s.StartTime) &&
                    (
                        s.FacultyId == dto.FacultyId ||
                        s.RoomId == dto.RoomId ||
                        s.ClassSectionId == dto.ClassSectionId
                    ) &&
                    // Exclude the current schedule if editing
                    (!dto.Id.HasValue || s.Id != dto.Id.Value)
                )
                .Include(s => s.Faculty)
                .Include(s => s.Room)
                .Include(s => s.ClassSection)
                .Include(s => s.Subject)
                .ToListAsync();

            if (!conflicts.Any())
            {
                return Ok(new ConflictCheckResultDto
                {
                    HasConflict = false,
                    ConflictingResources = new List<string>()
                });
            }

            // Build detailed conflict messages
            var conflictMessages = new List<string>();

            foreach (var conflict in conflicts)
            {
                var conflictDetails = new List<string>();

                if (conflict.FacultyId == dto.FacultyId)
                {
                    conflictDetails.Add($"Faculty ({conflict.Faculty.FullName})");
                }

                if (conflict.RoomId == dto.RoomId)
                {
                    conflictDetails.Add($"Room ({conflict.Room.Name})");
                }

                if (conflict.ClassSectionId == dto.ClassSectionId)
                {
                    conflictDetails.Add($"Section ({conflict.ClassSection.Section})");
                }

                if (conflictDetails.Any())
                {
                    var timeRange = $"{conflict.StartTime:hh\\:mm}-{conflict.EndTime:hh\\:mm}";
                    var message = $"{string.Join(" & ", conflictDetails)} already scheduled ({timeRange})";
                    conflictMessages.Add(message);
                }
            }

            return Ok(new ConflictCheckResultDto
            {
                HasConflict = true,
                ConflictingResources = conflictMessages.Distinct().ToList()
            });
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

        // GET: api/schedule/print?pov=Faculty&id=abc123&semesterId=1&day=Monday
        [HttpGet("print")]
        public async Task<IActionResult> PrintSchedule(
            [FromQuery] string pov,
            [FromQuery] string? id,  //
            [FromQuery] int? semesterId,
            [FromQuery] DayOfWeek? day)
        {
            // ✅ FIX: Only require id if POV is not "All"
            if (string.IsNullOrWhiteSpace(pov))
                return BadRequest("POV parameter is required.");

            var isAllPov = pov.Equals("All", StringComparison.OrdinalIgnoreCase);

            if (!isAllPov && string.IsNullOrWhiteSpace(id))
                return BadRequest("ID is required when POV is not 'All'.");

            IQueryable<Schedule> baseQuery = pov.ToLower() switch
            {
                "faculty" => _context.Schedules
                    .Where(s => s.FacultyId == id && (!semesterId.HasValue || s.ClassSection.SemesterId == semesterId)),

                "classsection" => _context.Schedules
                    .Where(s => s.ClassSectionId == int.Parse(id!) && (!semesterId.HasValue || s.ClassSection.SemesterId == semesterId)),

                "room" => _context.Schedules
                    .Where(s => s.RoomId == int.Parse(id!) && (!semesterId.HasValue || s.ClassSection.SemesterId == semesterId)),

                "all" => _context.Schedules
                    .Where(s => !semesterId.HasValue || s.ClassSection.SemesterId == semesterId),

                _ => null!
            };

            if (baseQuery == null)
                return BadRequest("Invalid POV.");

            // Apply day filter if provided
            if (day.HasValue)
            {
                baseQuery = baseQuery.Where(s => s.Day == day.Value);
            }

            // Include all navigation properties using your extension
            var schedules = await baseQuery.IncludeAll().ToListAsync();

            // ✅ FIX: Better error message
            if (!schedules.Any())
                return NotFound($"No schedules found for the selected criteria ({pov}, Semester: {semesterId?.ToString() ?? "All"}, Day: {day?.ToString() ?? "All"}).");

            // ✅ FIX: Use "All" as id when pov is "All"
            var pdfBytes = _pdfService.GenerateSchedulePdf(schedules, pov, id ?? "All");

            // Add day to filename when provided for clarity
            var dayLabel = day.HasValue ? $"_{day.Value}" : string.Empty;
            var idLabel = string.IsNullOrWhiteSpace(id) ? "All" : id;

            return File(pdfBytes, "application/pdf", $"Schedule_{pov}_{idLabel}{dayLabel}_Sem{semesterId}.pdf");
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
            SubjectHours = s.Subject.Hours,
            SubjectColor = s.Subject.Color ?? "#999999",

            ClassSectionId = s.ClassSectionId,
            ClassSectionName = s.ClassSection.Section,
            CourseCode = s.ClassSection.CollegeCourse.Code,
            YearLevel = s.ClassSection.YearLevel.ToString(),

            SemesterName = s.ClassSection.Semester.Name,
            SchoolYearLabel = $"{s.ClassSection.Semester.SchoolYear.StartYear}-{s.ClassSection.Semester.SchoolYear.EndYear}",

            IsActive = s.IsActive
        };

        // Put this inside the ScheduleController class (not outside it)
        private ScheduleNotificationInfo BuildNotifyInfo(Schedule s)
        {
            // safe guards for null navigation properties and readable semester label
            var semesterLabel = s.ClassSection?.Semester?.SchoolYear != null
                ? $"{s.ClassSection.Semester.SchoolYear.StartYear}-{s.ClassSection.Semester.SchoolYear.EndYear}"
                : s.ClassSection?.Semester?.Name ?? string.Empty;

            return new ScheduleNotificationInfo(
                ScheduleId: s.Id,
                SubjectCode: s.Subject?.SubjectCode ?? string.Empty,
                SubjectTitle: s.Subject?.SubjectTitle ?? string.Empty,
                SectionLabel: s.ClassSection?.Section ?? string.Empty,
                Day: s.Day.ToString(),
                StartTime: s.StartTime.ToString(@"hh\:mm"),
                EndTime: s.EndTime.ToString(@"hh\:mm"),
                Room: s.Room?.Name ?? "TBA",
                CourseCode: s.ClassSection?.CollegeCourse?.Code ?? string.Empty,
                SemesterLabel: semesterLabel
            );
        }
    }

    public static class ScheduleIncludesExtension
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
