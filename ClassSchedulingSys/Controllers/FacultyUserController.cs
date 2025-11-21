using ClassSchedulingSys.Data;
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using ClassSchedulingSys.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")]
    public class FacultyUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISchedulePdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public FacultyUserController(ApplicationDbContext context, ISchedulePdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _pdfService = pdfService;
            _environment = environment;
        }

        // GET: api/facultyuser/my-schedule
        [HttpGet("my-schedule")]
        public async Task<ActionResult<IEnumerable<ScheduleReadDto>>> GetMySchedule()
        {
            // 🔒 Correctly extract faculty ID from standard claims
            var facultyId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (facultyId == null)
                return Unauthorized("Faculty ID not found in token claims.");

            var schedules = await _context.Schedules
                .Where(s => s.FacultyId == facultyId)
                .IncludeAll()
                .ToListAsync();

            return Ok(schedules.Select(MapToReadDto));
        }

        // GET: api/facultyuser/assigned-subjects
        [HttpGet("assigned-subjects")]
        public async Task<ActionResult<IEnumerable<object>>> GetAssignedSubjects()
        {
            var facultyId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (facultyId == null) return Unauthorized("Faculty ID not found in token claims.");

            var assignments = await _context.FacultySubjectAssignments
                .Where(a => a.FacultyId == facultyId)
                .Include(a => a.Subject)
                .Include(a => a.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(a => a.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(s => s.SchoolYear)
                .ToListAsync();

            var result = assignments.Select(a => new
            {
                a.SubjectId,
                a.Subject.SubjectTitle,
                a.Subject.SubjectCode,
                a.Subject.Units,
                a.ClassSectionId,
                a.ClassSection.Section,
                a.ClassSection.YearLevel,
                CourseCode = a.ClassSection.CollegeCourse.Code,
                Semester = a.ClassSection.Semester.Name,
                SchoolYear = $"{a.ClassSection.Semester.SchoolYear.StartYear}-{a.ClassSection.Semester.SchoolYear.EndYear}"
            });

            return Ok(result);
        }

        // GET: api/facultyuser/print-my-schedule
        [HttpGet("print-my-schedule")]
        public async Task<IActionResult> PrintMySchedule([FromQuery] int? semesterId, [FromQuery] DayOfWeek? day)
        {
            var facultyId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (facultyId == null)
                return Unauthorized("Faculty ID not found in token claims.");

            // Base query: schedules for this faculty
            var query = _context.Schedules
                .Where(s => s.FacultyId == facultyId);

            // Apply semester filter if provided
            if (semesterId.HasValue)
            {
                query = query.Where(s => s.ClassSection.SemesterId == semesterId.Value);
            }

            // NEW: Apply day filter if provided
            if (day.HasValue)
            {
                query = query.Where(s => s.Day == day.Value);
            }

            // Include all necessary navigation properties
            var schedules = await query.IncludeAll().ToListAsync();

            if (!schedules.Any())
                return NotFound("No schedules found.");

            // Generate PDF with appropriate label
            var pdfBytes = _pdfService.GenerateSchedulePdf(schedules, "Faculty", facultyId);

            // Create filename with day label if filtered
            var dayLabel = day.HasValue ? $"_{day.Value}" : string.Empty;
            var filename = $"Schedule_Faculty_{facultyId}{dayLabel}_Sem{semesterId}.pdf";

            return File(pdfBytes, "application/pdf", filename);
        }


        // GET: api/facultyuser/current-semester
        [HttpGet("current-semester")]
        public async Task<ActionResult<SemesterDto>> GetCurrentSemester()
        {
            var semester = await _context.Semesters
                .Include(s => s.SchoolYear)
                .Where(s => s.IsCurrent)
                .Select(s => new SemesterDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    SchoolYearId = s.SchoolYearId,
                    IsCurrent = s.IsCurrent,
                    IsSchoolYearCurrent = s.SchoolYear != null && s.SchoolYear.IsCurrent,
                    SchoolYearLabel = s.SchoolYear != null
                        ? $"{s.SchoolYear.StartYear}-{s.SchoolYear.EndYear}"
                        : null
                })
                .FirstOrDefaultAsync();

            if (semester == null)
                return NotFound("No current semester found.");

            return Ok(semester);
        }

        [HttpGet("my-schedule-grid")]
        public async Task<IActionResult> DownloadMyScheduleGrid([FromQuery] int? semesterId)
        {
            var facultyId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                            User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (facultyId == null)
                return Unauthorized("Faculty ID not found in token claims.");

            // Get faculty info
            var faculty = await _context.Users.FindAsync(facultyId);
            if (faculty == null)
                return NotFound("Faculty member not found.");

            // Build query for faculty schedules
            var query = _context.Schedules.Where(s => s.FacultyId == facultyId);

            // Apply semester filter if provided
            if (semesterId.HasValue)
            {
                query = query.Where(s => s.ClassSection.SemesterId == semesterId.Value);
            }

            // Load schedules with all navigation properties
            var schedules = await query.IncludeAll().ToListAsync();

            if (!schedules.Any())
                return NotFound("No schedules found.");

            // Get semester info
            var firstSchedule = schedules.First();
            var semesterName = firstSchedule.ClassSection?.Semester?.Name ?? "Current Semester";
            var schoolYear = firstSchedule.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "N/A";

            // Generate Excel using the new service
            var gridService = new FacultyScheduleGridExcelService();
            var excelBytes = gridService.GenerateFacultyScheduleGrid(
                schedules,
                faculty.FullName,
                faculty.EmployeeID ?? "N/A",
                semesterName,
                syLabel);

            var filename = $"My_Schedule_Grid_Sem{semesterId}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        // Helper for mapping
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

        /// <summary>
        /// Download current faculty's schedule grid as PDF (Landscape Legal)
        /// </summary>
        [HttpGet("my-schedule-grid-pdf")]
        public async Task<IActionResult> DownloadMyScheduleGridPdf([FromQuery] int? semesterId)
        {
            var facultyId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                           User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (facultyId == null)
                return Unauthorized("Faculty ID not found in token claims.");

            // Get faculty info
            var faculty = await _context.Users.FindAsync(facultyId);
            if (faculty == null)
                return NotFound("Faculty member not found.");

            // Build query for faculty schedules
            var query = _context.Schedules.Where(s => s.FacultyId == facultyId);

            // Apply semester filter if provided
            if (semesterId.HasValue)
            {
                query = query.Where(s => s.ClassSection.SemesterId == semesterId.Value);
            }

            // Load schedules with all navigation properties
            var schedules = await query.IncludeAll().ToListAsync();

            if (!schedules.Any())
                return NotFound("No schedules found for the selected semester.");

            // Get semester info
            var firstSchedule = schedules.First();
            var semesterName = firstSchedule.ClassSection?.Semester?.Name ?? "Current Semester";
            var schoolYear = firstSchedule.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "N/A";

            // Generate PDF using the new service
            var gridService = new FacultyScheduleGridPdfService(_environment);
            var pdfBytes = gridService.GenerateFacultyScheduleGridPdf(
                schedules,
                faculty.FullName,
                faculty.EmployeeID ?? "N/A",
                semesterName,
                syLabel);

            var filename = $"My_Schedule_Grid_Sem{semesterId}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", filename);
        }
    }
}
