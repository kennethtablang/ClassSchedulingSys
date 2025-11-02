using ClassSchedulingSys.Data;
using ClassSchedulingSys.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")] // show on dashboard to authorized users
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns unassigned subjects grouped by class section.
        /// Optional filters: semesterId and schoolYear (format "2025-2026" or omitted)
        /// </summary>
        [HttpGet("unassigned-subjects")]
        public async Task<ActionResult<UnassignedSubjectsDashboardDto>> GetUnassignedSubjects(
            [FromQuery] int? semesterId,
            [FromQuery] string? schoolYear)
        {
            // Query class sections, include related course/semester/schoolyear for labels
            var sectionsQuery = _context.ClassSections
                .Include(cs => cs.CollegeCourse)
                .Include(cs => cs.Semester)
                    .ThenInclude(s => s.SchoolYear)
                .AsQueryable();

            if (semesterId.HasValue)
            {
                sectionsQuery = sectionsQuery.Where(cs => cs.SemesterId == semesterId.Value);
            }

            if (!string.IsNullOrWhiteSpace(schoolYear) && schoolYear.Contains('-'))
            {
                var parts = schoolYear.Split('-');
                if (int.TryParse(parts[0], out int startYear) && int.TryParse(parts[1], out int endYear))
                {
                    sectionsQuery = sectionsQuery.Where(cs => cs.SchoolYearId == cs.SchoolYearId &&
                        cs.SchoolYear != null &&
                        cs.SchoolYear.StartYear == startYear && cs.SchoolYear.EndYear == endYear);
                    // The above includes a redundant predicate `cs.SchoolYearId == cs.SchoolYearId` to satisfy EF navigation - safe to keep
                }
            }

            var sections = await sectionsQuery.ToListAsync();

            var result = new UnassignedSubjectsDashboardDto();

            // For efficiency: preload assignments into a lookup keyed by (SubjectId, ClassSectionId)
            var assignmentSet = await _context.FacultySubjectAssignments
                .Select(a => new { a.SubjectId, a.ClassSectionId })
                .ToListAsync();

            var assignmentLookup = assignmentSet
                .GroupBy(a => a.ClassSectionId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.SubjectId).ToHashSet());

            // Preload Subjects by CollegeCourseId + YearLevel grouping to speed lookups
            var allSubjects = await _context.Subjects
                .Where(s => s.IsActive) // only consider active subjects
                .ToListAsync();

            foreach (var cs in sections)
            {
                // find candidate subjects that logically belong to this section:
                // subject.CollegeCourseId == cs.CollegeCourseId && subject.YearLevel == cs.YearLevel (string)
                var candidates = allSubjects
                    .Where(s =>
                        s.CollegeCourseId == cs.CollegeCourseId &&
                        string.Equals(s.YearLevel?.ToString() ?? s.YearLevel, cs.YearLevel.ToString(), StringComparison.OrdinalIgnoreCase)
                    // note: s.YearLevel is stored as string like "1st Year", while cs.YearLevel is int.
                    // To be more robust attempt to match numeric part if possible below.
                    )
                    .ToList();

                // If direct string equality failed for most projects, attempt numeric match fallback:
                if (!candidates.Any())
                {
                    // try to match subjects whose YearLevel contains the numeric cs.YearLevel (e.g. "1st Year" contains "1")
                    candidates = allSubjects
                        .Where(s => s.CollegeCourseId == cs.CollegeCourseId &&
                                    !string.IsNullOrWhiteSpace(s.YearLevel) &&
                                    s.YearLevel.Contains(cs.YearLevel.ToString()))
                        .ToList();
                }

                // Now filter out ones that already have assignments for this class section
                var assignedForSection = assignmentLookup.ContainsKey(cs.Id) ? assignmentLookup[cs.Id] : new HashSet<int>();

                var unassigned = candidates
                    .Where(s => !assignedForSection.Contains(s.Id))
                    .Select(s => new UnassignedSubjectDto
                    {
                        SubjectId = s.Id,
                        SubjectCode = s.SubjectCode,
                        SubjectTitle = s.SubjectTitle,
                        Units = s.Units,
                        SubjectType = s.SubjectType,
                        YearLevel = s.YearLevel,
                        Color = s.Color
                    })
                    .OrderBy(s => s.SubjectCode)
                    .ToList();

                if (unassigned.Any())
                {
                    var dto = new UnassignedBySectionDto
                    {
                        ClassSectionId = cs.Id,
                        SectionLabel = cs.Section,
                        YearLevel = cs.YearLevel,
                        CollegeCourseId = cs.CollegeCourseId,
                        CollegeCourseCode = cs.CollegeCourse?.Code ?? string.Empty,
                        CollegeCourseName = cs.CollegeCourse?.Name ?? string.Empty,
                        SemesterId = cs.SemesterId,
                        SemesterName = cs.Semester?.Name ?? string.Empty,
                        SchoolYearLabel = cs.SchoolYear != null ? $"{cs.SchoolYear.StartYear}-{cs.SchoolYear.EndYear}" : string.Empty,
                        TotalUnassigned = unassigned.Count,
                        Subjects = unassigned
                    };

                    result.Sections.Add(dto);
                    result.TotalUnassignedAcrossAllSections += dto.TotalUnassigned;
                }
            }

            return Ok(result);
        }
    }
}
