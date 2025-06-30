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
    [Authorize(Roles = "Dean,SuperAdmin")]
    public class FacultyController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public FacultyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get all users with Faculty role
        /// </summary>
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
                IsActive = f.IsActive
            });

            return Ok(result);
        }

        /// <summary>
        /// Get all subject-section assignments for a faculty with total units
        /// </summary>
        [HttpGet("{facultyId}/assigned-subjects")]
        public async Task<IActionResult> GetAssignedSubjectsWithSections(string facultyId)
        {
            var assignments = await _context.FacultySubjectAssignments
                .Where(fsa => fsa.FacultyId == facultyId)
                .Include(fsa => fsa.Subject)
                .Include(fsa => fsa.ClassSection)
                .ThenInclude(cs => cs.CollegeCourse)
                .Select(fsa => new
                {
                    fsa.Subject.Id,
                    fsa.Subject.SubjectCode,
                    fsa.Subject.SubjectTitle,
                    fsa.Subject.Units,
                    fsa.Subject.SubjectType,
                    fsa.Subject.YearLevel,
                    fsa.ClassSectionId,
                    SectionLabel = fsa.ClassSection.Section,
                    CollegeCourseName = fsa.ClassSection.CollegeCourse.Name
                })
                .ToListAsync();

            var totalUnits = assignments.Sum(a => a.Units);
            var totalSubjects = assignments.Select(a => a.Id).Distinct().Count();

            return Ok(new
            {
                TotalUnits = totalUnits,
                TotalSubjects = totalSubjects,
                Subjects = assignments
            });
        }

        /// <summary>
        /// Overwrite-style: Assign subjects to a faculty per section
        /// </summary>
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

        /// <summary>
        /// Unassign a specific subject-section pair from a faculty
        /// </summary>
        [HttpDelete("{facultyId}/subject/{subjectId}/section/{sectionId}")]
        public async Task<IActionResult> UnassignSubjectFromSection(string facultyId, int subjectId, int sectionId)
        {
            var assignment = await _context.FacultySubjectAssignments
                .FirstOrDefaultAsync(a => a.FacultyId == facultyId && a.SubjectId == subjectId && a.ClassSectionId == sectionId);

            if (assignment == null)
                return NotFound("Assignment not found.");

            _context.FacultySubjectAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return Ok("Subject unassigned from section.");
        }

        [HttpGet("assigned-subjects")]
        public async Task<IActionResult> GetAllAssignedSubjects()
        {
            var assignments = await _context.FacultySubjectAssignments
                .Include(fss => fss.Faculty)
                .Select(fss => new AssignedSubjectInfoDto
                {
                    SubjectId = fss.SubjectId,
                    ClassSectionId = fss.ClassSectionId,
                    FacultyName = fss.Faculty.FullName
                })
                .ToListAsync();

            return Ok(assignments);
        }

    }
}
