// ClassSchedulingSys/Controllers/ExportController.cs
using ClassSchedulingSys.Data;
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
    public class ExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IScheduleExcelService _excelService;

        public ExportController(ApplicationDbContext context, IScheduleExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
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

    }
}