using ClassSchedulingSys.Data;
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")]
    public class SubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/subject
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectReadDto>>> GetAll()
        {
            var subjects = await _context.Subjects
                .Where(s => s.IsActive)
                .Include(s => s.CollegeCourse)
                .Select(s => new SubjectReadDto
                {
                    Id = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectTitle = s.SubjectTitle,
                    Units = s.Units,
                    Hours = s.Hours,
                    SubjectType = s.SubjectType,
                    YearLevel = s.YearLevel,
                    CollegeCourseId = s.CollegeCourseId,
                    CollegeCourseName = s.CollegeCourse.Name,
                    Color = s.Color,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return Ok(subjects);
        }

        // GET: api/subject/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SubjectReadDto>> GetById(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.CollegeCourse)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound("Subject not found.");

            var dto = new SubjectReadDto
            {
                Id = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectTitle = subject.SubjectTitle,
                Units = subject.Units,
                Hours = subject.Hours,
                SubjectType = subject.SubjectType,
                YearLevel = subject.YearLevel,
                CollegeCourseId = subject.CollegeCourseId,
                CollegeCourseName = subject.CollegeCourse?.Name ?? "",
                Color = subject.Color,
                IsActive = subject.IsActive
            };

            return Ok(dto);
        }

        // POST: api/subject
        [HttpPost]
        public async Task<IActionResult> Create(SubjectCreateDto dto)
        {
            // ✅ Check if subject code is already taken (globally unique, active or archived)
            var existingSubject = await _context.Subjects
                .Where(s => s.SubjectCode == dto.SubjectCode && s.IsActive)
                .FirstOrDefaultAsync();

            if (existingSubject != null)
            {
                return BadRequest($"Subject code '{dto.SubjectCode}' is already taken by another subject.");
            }

            var subject = new Subject
            {
                SubjectCode = dto.SubjectCode,
                SubjectTitle = dto.SubjectTitle,
                Units = dto.Units,
                Hours = dto.Hours,
                SubjectType = dto.SubjectType,
                YearLevel = dto.YearLevel,
                CollegeCourseId = dto.CollegeCourseId,
                Color = dto.Color ?? "#999999",
                IsActive = true
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject created successfully." });
        }

        // PUT: api/subject/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, SubjectUpdateDto dto)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound("Subject not found.");

            // ✅ Check if another ACTIVE subject has the same code, year, and course
            var duplicate = await _context.Subjects
                .Where(s => s.Id != id
                    && s.IsActive
                    && s.SubjectCode == dto.SubjectCode
                    && s.YearLevel == dto.YearLevel
                    && s.CollegeCourseId == dto.CollegeCourseId)
                .FirstOrDefaultAsync();

            if (duplicate != null)
            {
                return BadRequest($"Another subject with code '{dto.SubjectCode}' already exists for {dto.YearLevel} in this course.");
            }

            subject.SubjectCode = dto.SubjectCode;
            subject.SubjectTitle = dto.SubjectTitle;
            subject.Units = dto.Units;
            subject.Hours = dto.Hours;
            subject.SubjectType = dto.SubjectType;
            subject.YearLevel = dto.YearLevel;
            subject.CollegeCourseId = dto.CollegeCourseId;
            subject.Color = dto.Color ?? subject.Color;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Subject updated successfully." });
        }

        // DELETE: api/subject/{id} - Soft delete (archive)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound("Subject not found.");

            subject.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject archived successfully." });
        }

        // GET: api/subject/archived
        [HttpGet("archived")]
        public async Task<ActionResult<IEnumerable<SubjectReadDto>>> GetArchived()
        {
            var archived = await _context.Subjects
                .Where(s => !s.IsActive)
                .Include(s => s.CollegeCourse)
                .Select(s => new SubjectReadDto
                {
                    Id = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectTitle = s.SubjectTitle,
                    Units = s.Units,
                    Hours = s.Hours,
                    SubjectType = s.SubjectType,
                    YearLevel = s.YearLevel,
                    CollegeCourseId = s.CollegeCourseId,
                    CollegeCourseName = s.CollegeCourse.Name,
                    Color = s.Color,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return Ok(archived);
        }

        // PUT: api/subject/{id}/restore
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.CollegeCourse)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
                return NotFound("Subject not found.");

            if (subject.IsActive)
                return BadRequest("Subject is already active.");

            // ✅ Check if an active subject with same code, year, and course exists
            var existingActive = await _context.Subjects
                .Where(s => s.IsActive
                    && s.SubjectCode == subject.SubjectCode
                    && s.YearLevel == subject.YearLevel
                    && s.CollegeCourseId == subject.CollegeCourseId)
                .FirstOrDefaultAsync();

            if (existingActive != null)
            {
                return BadRequest($"Cannot restore: A subject with code '{subject.SubjectCode}' already exists for {subject.YearLevel} in {subject.CollegeCourse?.Name}. Please archive the existing subject first.");
            }

            subject.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subject restored successfully." });
        }
    }
}