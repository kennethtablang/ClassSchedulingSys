// ClassSchedulingSys/Controllers/ReportController.cs
using ClassSchedulingSys.Data;
using ClassSchedulingSys.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClassSchedulingSys.Services;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")]
    public class ReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISchedulePdfService _pdfService;

        public ReportController(ApplicationDbContext context, ISchedulePdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        /// <summary>
        /// Get faculty load summary for display (total units, total subjects per faculty)
        /// </summary>
        [HttpGet("faculty-load/summary")]
        public async Task<IActionResult> GetFacultyLoadSummary([FromQuery] int? semesterId)
        {
            var query = _context.FacultySubjectAssignments
                .Include(fsa => fsa.Faculty)
                .Include(fsa => fsa.Subject)
                .Include(fsa => fsa.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                .AsQueryable();

            // Filter by semester if provided
            if (semesterId.HasValue)
            {
                query = query.Where(fsa => fsa.ClassSection.SemesterId == semesterId.Value);
            }

            var assignments = await query.ToListAsync();

            // Group by faculty and calculate totals
            var summary = assignments
                .GroupBy(fsa => new
                {
                    FacultyId = fsa.FacultyId,
                    FacultyName = fsa.Faculty.FullName,
                    EmployeeID = fsa.Faculty.EmployeeID
                })
                .Select(g => new
                {
                    FacultyId = g.Key.FacultyId,
                    FacultyName = g.Key.FacultyName,
                    EmployeeID = g.Key.EmployeeID,
                    TotalUnits = g.Sum(a => a.Subject.Units),
                    TotalSubjects = g.Select(a => a.SubjectId).Distinct().Count()
                })
                .OrderBy(x => x.FacultyName)
                .ToList();

            return Ok(summary);
        }

        /// <summary>
        /// Download PDF report for all faculty members (one page per faculty)
        /// </summary>
        [HttpGet("faculty-load/all")]
        public async Task<IActionResult> DownloadAllFacultyLoadReport([FromQuery] int? semesterId)
        {
            // Get all active faculty members
            var facultyUsers = await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            var facultyIds = facultyUsers.Select(f => f.Id).ToList();

            // Get all assignments for these faculty
            var query = _context.FacultySubjectAssignments
                .Include(fsa => fsa.Faculty)
                .Include(fsa => fsa.Subject)
                .Include(fsa => fsa.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(fsa => fsa.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(s => s.SchoolYear)
                .Where(fsa => facultyIds.Contains(fsa.FacultyId))
                .AsQueryable();

            if (semesterId.HasValue)
            {
                query = query.Where(fsa => fsa.ClassSection.SemesterId == semesterId.Value);
            }

            var assignments = await query.ToListAsync();

            if (!assignments.Any())
                return NotFound("No faculty load data found for the selected semester.");

            // Generate multi-page PDF (one page per faculty)
            var pdfBytes = _pdfService.GenerateFacultyLoadReport(assignments, semesterId);

            var semesterLabel = semesterId.HasValue ? $"Sem{semesterId}" : "Current";
            return File(pdfBytes, "application/pdf", $"Faculty_Load_Report_All_{semesterLabel}.pdf");
        }

        /// <summary>
        /// Download PDF report for a specific faculty member
        /// </summary>
        [HttpGet("faculty-load/{facultyId}")]
        public async Task<IActionResult> DownloadFacultyLoadReport(string facultyId, [FromQuery] int? semesterId)
        {
            // Get faculty info
            var faculty = await _context.Users.FindAsync(facultyId);
            if (faculty == null)
                return NotFound("Faculty member not found.");

            // Get assignments for this faculty
            var query = _context.FacultySubjectAssignments
                .Include(fsa => fsa.Faculty)
                .Include(fsa => fsa.Subject)
                .Include(fsa => fsa.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(fsa => fsa.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                        .ThenInclude(s => s.SchoolYear)
                .Where(fsa => fsa.FacultyId == facultyId)
                .AsQueryable();

            if (semesterId.HasValue)
            {
                query = query.Where(fsa => fsa.ClassSection.SemesterId == semesterId.Value);
            }

            var assignments = await query.ToListAsync();

            if (!assignments.Any())
                return NotFound($"No load data found for {faculty.FullName} in the selected semester.");

            // Generate single-page PDF
            var pdfBytes = _pdfService.GenerateFacultyLoadReport(assignments, semesterId);

            var semesterLabel = semesterId.HasValue ? $"Sem{semesterId}" : "Current";
            return File(pdfBytes, "application/pdf", $"Faculty_Load_{faculty.FullName}_{semesterLabel}.pdf");
        }
    }
}