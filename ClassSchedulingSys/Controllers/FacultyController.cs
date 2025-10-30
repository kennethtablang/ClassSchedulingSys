using ClassSchedulingSys.Data;
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")]
    public class FacultyController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public FacultyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFaculty()
        {
            var facultyUsers = await _userManager.GetUsersInRoleAsync("Faculty");

            var result = facultyUsers.Select(f => new FacultyReadDto
            {
                Id = f.Id,
                FullName = f.FullName,
                Email = f.Email,
                PhoneNumber = f.PhoneNumber,
                DepartmentId = f.DepartmentId,
                IsActive = f.IsActive,
                EmployeeID = f.EmployeeID
            });

            return Ok(result);
        }

        [HttpGet("{facultyId}/assigned-subjects")]
        public async Task<IActionResult> GetAssignedSubjectsWithSections(
            string facultyId,
            [FromQuery] int? semesterId,
            [FromQuery] string? schoolYear // format: "2025-2026"
)
        {
            var query = _context.FacultySubjectAssignments
                .Where(fsa => fsa.FacultyId == facultyId)
                .Include(fsa => fsa.Subject)
                .Include(fsa => fsa.ClassSection)
                    .ThenInclude(cs => cs.CollegeCourse)
                .Include(fsa => fsa.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                .Include(fsa => fsa.ClassSection)
                    .ThenInclude(cs => cs.SchoolYear)
                .AsQueryable();

            if (semesterId.HasValue)
            {
                query = query.Where(fsa => fsa.ClassSection.SemesterId == semesterId.Value);
            }

            if (!string.IsNullOrWhiteSpace(schoolYear) && schoolYear.Contains('-'))
            {
                var parts = schoolYear.Split('-');
                if (int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
                {
                    query = query.Where(fsa =>
                        fsa.ClassSection.SchoolYear.StartYear == startYear &&
                        fsa.ClassSection.SchoolYear.EndYear == endYear);
                }
            }

            var assignments = await query.ToListAsync();

            var assignmentDtos = assignments.Select(a => new
            {
                a.Subject.Id,
                a.Subject.SubjectCode,
                a.Subject.SubjectTitle,
                a.Subject.Units,
                a.Subject.SubjectType,
                a.Subject.YearLevel,
                a.ClassSectionId,
                SectionLabel = a.ClassSection.Section,
                CollegeCourseName = a.ClassSection.CollegeCourse.Name,
                SemesterId = a.ClassSection.SemesterId,
                SemesterName = a.ClassSection.Semester.Name,
                SchoolYearLabel = a.ClassSection.SchoolYear.StartYear + "-" + a.ClassSection.SchoolYear.EndYear
            }).ToList();

            var totalUnits = assignmentDtos.Sum(a => a.Units);
            var totalSubjects = assignmentDtos.Select(a => a.Id).Distinct().Count();

            return Ok(new
            {
                TotalUnits = totalUnits,
                TotalSubjects = totalSubjects,
                Subjects = assignmentDtos
            });
        }


        [HttpPost("assign-subjects-per-section")]
        public async Task<IActionResult> AssignSubjectsPerSection([FromBody] AssignSubjectsToFacultyPerSectionDto dto)
        {
            var faculty = await _context.Users
                .Include(f => f.FacultySubjectAssignments)
                .FirstOrDefaultAsync(f => f.Id == dto.FacultyId);

            if (faculty == null)
                return NotFound("Faculty not found.");

            // Remove old assignments
            var existing = _context.FacultySubjectAssignments
                .Where(fsa => fsa.FacultyId == dto.FacultyId);
            _context.FacultySubjectAssignments.RemoveRange(existing);
            await _context.SaveChangesAsync();

            // Add new assignments
            var newAssignments = dto.Assignments.Select(a => new FacultySubjectAssignment
            {
                FacultyId = dto.FacultyId,
                SubjectId = a.SubjectId,
                ClassSectionId = a.ClassSectionId
            });

            await _context.FacultySubjectAssignments.AddRangeAsync(newAssignments);
            await _context.SaveChangesAsync();

            return Ok("Subjects assigned per section successfully.");
        }

        [HttpDelete("{facultyId}/subject/{subjectId}/section/{sectionId}")]
        public async Task<IActionResult> UnassignSubjectFromSection(string facultyId, int subjectId, int sectionId)
        {
            var assignment = await _context.FacultySubjectAssignments
                .FirstOrDefaultAsync(a =>
                    a.FacultyId == facultyId &&
                    a.SubjectId == subjectId &&
                    a.ClassSectionId == sectionId);

            if (assignment == null)
                return NotFound("Assignment not found.");

            _context.FacultySubjectAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return Ok("Subject unassigned from section.");
        }

        [HttpGet("assigned-subjects")]
        public async Task<IActionResult> GetAllAssignedSubjects(
    [FromQuery] int? semesterId,
    [FromQuery] string? schoolYear)
        {
            var query = _context.FacultySubjectAssignments
                .Include(fss => fss.Faculty)
                .Include(fss => fss.Subject)
                .Include(fss => fss.ClassSection)
                    .ThenInclude(cs => cs.Semester)
                .Include(fss => fss.ClassSection)
                    .ThenInclude(cs => cs.SchoolYear)
                .AsQueryable();

            if (semesterId.HasValue)
            {
                query = query.Where(fsa => fsa.ClassSection.SemesterId == semesterId.Value);
            }

            if (!string.IsNullOrWhiteSpace(schoolYear) && schoolYear.Contains("-"))
            {
                var parts = schoolYear.Split("-");
                if (int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
                {
                    query = query.Where(fsa =>
                        fsa.ClassSection.SchoolYear.StartYear == startYear &&
                        fsa.ClassSection.SchoolYear.EndYear == endYear);
                }
            }

            var assignments = await query
                .Select(fss => new
                {
                    SubjectId = fss.SubjectId,
                    SubjectTitle = fss.Subject.SubjectTitle,
                    ClassSectionId = fss.ClassSectionId,
                    ClassSectionName = fss.ClassSection.Section,
                    FacultyName = fss.Faculty.FullName,
                    SemesterId = fss.ClassSection.SemesterId,
                    SemesterName = fss.ClassSection.Semester.Name,
                    SchoolYearLabel = fss.ClassSection.SchoolYear.StartYear + "-" + fss.ClassSection.SchoolYear.EndYear
                })
                .ToListAsync();

            return Ok(assignments);
        }


        [HttpGet("classsection/{sectionId}/assignments")]
        public async Task<IActionResult> GetAssignmentsByClassSection(int sectionId)
        {
            var result = await _context.FacultySubjectAssignments
                .Where(fsa => fsa.ClassSectionId == sectionId)
                .Include(fsa => fsa.Faculty)
                .Include(fsa => fsa.Subject)
                .Select(fsa => new
                {
                    FacultyId = fsa.FacultyId,
                    FacultyName = fsa.Faculty.FullName,
                    SubjectId = fsa.SubjectId,
                    SubjectTitle = fsa.Subject.SubjectTitle,
                    Units = fsa.Subject.Units
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{facultyId}/unassigned-subjects")]
        public async Task<IActionResult> GetUnscheduledAssignmentsForFaculty(string facultyId)
        {
            var assigned = await _context.FacultySubjectAssignments
                .Where(fsa => fsa.FacultyId == facultyId)
                .ToListAsync();

            var scheduled = await _context.Schedules
                .Where(s => s.FacultyId == facultyId)
                .Select(s => new { s.SubjectId, s.ClassSectionId })
                .ToListAsync();

            var unscheduled = assigned
                .Where(a => !scheduled.Any(s => s.SubjectId == a.SubjectId && s.ClassSectionId == a.ClassSectionId))
                .Join(_context.Subjects, a => a.SubjectId, s => s.Id, (a, s) => new { a, s })
                .Join(_context.ClassSections, temp => temp.a.ClassSectionId, cs => cs.Id, (temp, cs) => new
                {
                    SubjectId = temp.a.SubjectId,
                    SubjectTitle = temp.s.SubjectTitle,
                    Units = temp.s.Units,
                    SubjectType = temp.s.SubjectType,
                    ClassSectionId = temp.a.ClassSectionId,
                    Section = cs.Section,
                    YearLevel = temp.s.YearLevel
                })
                .ToList();

            return Ok(unscheduled);
        }

        [HttpGet("{facultyId}/total-units")]
        public async Task<IActionResult> GetFacultyTotalUnits(string facultyId)
        {
            var total = await _context.FacultySubjectAssignments
                .Where(fsa => fsa.FacultyId == facultyId)
                .Join(_context.Subjects, a => a.SubjectId, s => s.Id, (a, s) => s.Units)
                .SumAsync();

            return Ok(new { FacultyId = facultyId, TotalUnits = total });
        }
    }
}
