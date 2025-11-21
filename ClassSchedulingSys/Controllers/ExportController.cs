// ClassSchedulingSys/Controllers/ExportController.cs
using ClassSchedulingSys.Data;
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using ClassSchedulingSys.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")]
    public class ExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IScheduleExcelService _excelService;
        private readonly ISchedulePdfService _pdfService;
        private readonly IWebHostEnvironment _environment;

        public ExportController(ApplicationDbContext context, IScheduleExcelService excelService, ISchedulePdfService pdfService, IWebHostEnvironment environment)
        {
            _context = context;
            _excelService = excelService;
            _pdfService = pdfService;
            _environment = environment;
        }

        /// <summary>
        /// Export schedule to Excel with worksheets per day
        /// </summary>
        [HttpGet("schedule/excel")]
        public async Task<IActionResult> ExportScheduleToExcel(
            [FromQuery] string pov,
            [FromQuery] string? id,
            [FromQuery] int? semesterId,
            [FromQuery] int? courseId,
            [FromQuery] int? yearLevel,
            [FromQuery] int? roomId,
            [FromQuery] DayOfWeek? day)
        {
            if (string.IsNullOrWhiteSpace(pov) || (pov.ToLower() != "all" && string.IsNullOrWhiteSpace(id)))
                return BadRequest("POV and ID are required (except for 'All').");

            // Build base query
            IQueryable<Schedule> query = pov.ToLower() switch
            {
                "faculty" => _context.Schedules.Where(s => s.FacultyId == id),
                "classsection" => _context.Schedules.Where(s => s.ClassSectionId == int.Parse(id!)),
                "room" => _context.Schedules.Where(s => s.RoomId == int.Parse(id!)),
                "all" => _context.Schedules,
                _ => null!
            };

            if (query == null)
                return BadRequest("Invalid POV.");

            // Apply filters
            if (semesterId.HasValue)
            {
                query = query.Where(s => s.ClassSection.SemesterId == semesterId.Value);
            }

            if (courseId.HasValue)
            {
                query = query.Where(s => s.ClassSection.CollegeCourseId == courseId.Value);
            }

            if (yearLevel.HasValue)
            {
                query = query.Where(s => s.ClassSection.YearLevel == yearLevel.Value);
            }

            if (roomId.HasValue)
            {
                query = query.Where(s => s.RoomId == roomId.Value);
            }

            if (day.HasValue)
            {
                query = query.Where(s => s.Day == day.Value);
            }

            // Load schedules with all navigation properties
            var schedules = await query.IncludeAll().ToListAsync();

            if (!schedules.Any())
                return NotFound("No schedules found for the specified criteria.");

            // Get semester info for labels
            var firstSchedule = schedules.First();
            var semesterName = firstSchedule.ClassSection?.Semester?.Name ?? "N/A";
            var schoolYear = firstSchedule.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "N/A";

            // Generate Excel
            var excelBytes = _excelService.GenerateScheduleExcel(
                schedules,
                pov,
                id ?? "All",
                semesterName,
                syLabel);

            var dayLabel = day.HasValue ? $"_{day.Value}" : "";
            var filename = $"Schedule_{pov}_{id ?? "All"}{dayLabel}_Sem{semesterId}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        /// <summary>
        /// Export room utilization report to Excel
        /// </summary>
        [HttpGet("room-utilization/excel")]
        public async Task<IActionResult> ExportRoomUtilization([FromQuery] int? semesterId)
        {
            // Load schedules with filters
            var query = _context.Schedules.AsQueryable();

            if (semesterId.HasValue)
            {
                query = query.Where(s => s.ClassSection.SemesterId == semesterId.Value);
            }

            var schedules = await query.IncludeAll().ToListAsync();

            if (!schedules.Any())
                return NotFound("No schedules found for the specified semester.");

            // Load all rooms
            var allRooms = await _context.Rooms
                .Include(r => r.Building)
                .ToListAsync();

            // Get semester info
            var firstSchedule = schedules.First();
            var semesterName = firstSchedule.ClassSection?.Semester?.Name ?? "Current Semester";
            var schoolYear = firstSchedule.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "N/A";

            // Generate Excel
            var excelBytes = _excelService.GenerateRoomUtilizationExcel(
                schedules,
                allRooms,
                semesterName,
                syLabel);

            var filename = $"RoomUtilization_Sem{semesterId}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        /// <summary>
        /// Export schedule for current faculty user
        /// </summary>
        [HttpGet("my-schedule/excel")]
        public async Task<IActionResult> ExportMySchedule([FromQuery] int? semesterId, [FromQuery] DayOfWeek? day)
        {
            var facultyId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                           User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            if (facultyId == null)
                return Unauthorized("Faculty ID not found in token claims.");

            var query = _context.Schedules.Where(s => s.FacultyId == facultyId);

            if (semesterId.HasValue)
            {
                query = query.Where(s => s.ClassSection.SemesterId == semesterId.Value);
            }

            if (day.HasValue)
            {
                query = query.Where(s => s.Day == day.Value);
            }

            var schedules = await query.IncludeAll().ToListAsync();

            if (!schedules.Any())
                return NotFound("No schedules found.");

            var firstSchedule = schedules.First();
            var semesterName = firstSchedule.ClassSection?.Semester?.Name ?? "N/A";
            var schoolYear = firstSchedule.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "N/A";

            var excelBytes = _excelService.GenerateScheduleExcel(
                schedules,
                "Faculty",
                facultyId,
                semesterName,
                syLabel);

            var dayLabel = day.HasValue ? $"_{day.Value}" : "";
            var filename = $"MySchedule{dayLabel}_Sem{semesterId}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        /// <summary>
        /// Export schedule in GRID format (time slots x rooms) like a visual timetable
        /// </summary>
        [HttpGet("schedule/grid-excel")]
        public async Task<IActionResult> ExportGridSchedule(
            [FromQuery] int? semesterId,
            [FromQuery] int? courseId,
            [FromQuery] int? yearLevel)
        {
            // Build query
            IQueryable<Schedule> query = _context.Schedules;

            if (semesterId.HasValue)
            {
                query = query.Where(s => s.ClassSection.SemesterId == semesterId.Value);
            }

            if (courseId.HasValue)
            {
                query = query.Where(s => s.ClassSection.CollegeCourseId == courseId.Value);
            }

            if (yearLevel.HasValue)
            {
                query = query.Where(s => s.ClassSection.YearLevel == yearLevel.Value);
            }

            // Load schedules with navigation properties
            var schedules = await query.IncludeAll().ToListAsync();

            if (!schedules.Any())
                return NotFound("No schedules found for the specified criteria.");

            // Get all rooms
            var rooms = await _context.Rooms
                .Include(r => r.Building)
                .OrderBy(r => r.Name)
                .ToListAsync();

            // Get semester info
            var firstSchedule = schedules.First();
            var semesterName = firstSchedule.ClassSection?.Semester?.Name ?? "N/A";
            var schoolYear = firstSchedule.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "N/A";

            // Generate grid Excel using the new service
            var gridService = new GridScheduleExcelService();
            var excelBytes = gridService.GenerateGridScheduleExcel(
                schedules,
                rooms,
                semesterName,
                syLabel);

            var filename = $"Schedule_Grid_Sem{semesterId}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        // Add this endpoint for Admin/Dean to download faculty schedule grid
        [HttpGet("faculty-schedule-grid/{facultyId}")]
        [Authorize(Roles = "Dean,SuperAdmin")]
        public async Task<IActionResult> ExportFacultyScheduleGrid(
            string facultyId,
            [FromQuery] int? semesterId)
        {
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
                return NotFound("No schedules found for this faculty member.");

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

            var filename = $"Faculty_Schedule_Grid_{faculty.FullName.Replace(" ", "_")}_Sem{semesterId}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }

        [HttpGet("schedule/course-block")]
        public async Task<IActionResult> ExportCourseBlockSchedule(
        [FromQuery] int courseId,
        [FromQuery] int yearLevel,
        [FromQuery] int semesterId,
        [FromQuery] string? section = null)
        {
            if (courseId == 0 || yearLevel == 0 || semesterId == 0)
                return BadRequest("Course ID, Year Level, and Semester ID are required.");

            // Build query to get schedules for this course, year, and section
            var query = _context.Schedules
                .Where(s =>
                    s.ClassSection.CollegeCourseId == courseId &&
                    s.ClassSection.YearLevel == yearLevel &&
                    s.ClassSection.SemesterId == semesterId);

            // Filter by specific section/block if provided
            if (!string.IsNullOrWhiteSpace(section))
            {
                query = query.Where(s => s.ClassSection.Section == section);
            }

            // Load schedules with all navigation properties
            var schedules = await query.IncludeAll().ToListAsync();

            if (!schedules.Any())
                return NotFound("No schedules found for the specified criteria.");

            // Get course and semester info for header
            var firstSchedule = schedules.First();
            var courseName = firstSchedule.ClassSection?.CollegeCourse?.Name ?? "N/A";
            var courseCode = firstSchedule.ClassSection?.CollegeCourse?.Code ?? "N/A";
            var semesterName = firstSchedule.ClassSection?.Semester?.Name ?? "N/A";
            var schoolYear = firstSchedule.ClassSection?.Semester?.SchoolYear;
            var syLabel = schoolYear != null ? $"{schoolYear.StartYear}-{schoolYear.EndYear}" : "N/A";

            var sectionLabel = string.IsNullOrWhiteSpace(section)
                ? "All Blocks"
                : $"Block {section}";

            // Generate PDF
            var pdfBytes = _pdfService.GenerateCourseBlockSchedulePdf(
                schedules,
                courseCode,
                courseName,
                yearLevel,
                sectionLabel,
                semesterName,
                syLabel);

            var filename = $"Schedule_{courseCode}_{yearLevel}Y_{section ?? "All"}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", filename);
        }

        /// <summary>
        /// Export faculty schedule grid as PDF for Admin/Dean (Landscape Legal)
        /// </summary>
        [HttpGet("faculty-schedule-grid-pdf/{facultyId}")]
        [Authorize(Roles = "Dean,SuperAdmin")]
        public async Task<IActionResult> ExportFacultyScheduleGridPdf(
            string facultyId,
            [FromQuery] int? semesterId)
        {
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
                return NotFound("No schedules found for this faculty member.");

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

            var filename = $"Faculty_Schedule_Grid_{faculty.FullName.Replace(" ", "_")}_Sem{semesterId}_{DateTime.Now:yyyyMMdd}.pdf";

            return File(pdfBytes, "application/pdf", filename);
        }

    }
}