// ClassSchedulingSys/Controllers/SubjectController
using AutoMapper;
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
    public class SubjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SubjectController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET: api/Subject
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubjectReadDto>>> GetSubjects()
        {
            var subjects = await _context.Subjects
                .Where(s => s.IsActive)
                .Include(s => s.CollegeCourse)
                .ToListAsync();

            var subjectDTOs = _mapper.Map<List<SubjectReadDto>>(subjects);
            return Ok(subjectDTOs);
        }

        // GET: api/Subject/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SubjectReadDto>> GetSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.CollegeCourse)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (subject == null)
                return NotFound("Subject not found.");

            return _mapper.Map<SubjectReadDto>(subject);
        }

        // POST: api/Subject
        [HttpPost]
        public async Task<ActionResult<SubjectReadDto>> CreateSubject(SubjectCreateDto dto)
        {
            try
            {
                // Basic normalization
                var code = dto.SubjectCode?.Trim();
                if (string.IsNullOrWhiteSpace(code))
                    return BadRequest("SubjectCode is required.");

                // Check if there's an archived subject with same code, year, and course
                var archivedSubject = await _context.Subjects
                    .FirstOrDefaultAsync(s =>
                        s.SubjectCode != null &&
                        s.SubjectCode.ToUpper() == code.ToUpper() &&
                        s.YearLevel == dto.YearLevel &&
                        s.CollegeCourseId == dto.CollegeCourseId &&
                        !s.IsActive);

                if (archivedSubject != null)
                {
                    // Restore the archived subject instead of creating new one
                    archivedSubject.SubjectTitle = dto.SubjectTitle;
                    archivedSubject.Units = dto.Units;
                    archivedSubject.Hours = dto.Hours;
                    archivedSubject.SubjectType = dto.SubjectType;
                    archivedSubject.Color = dto.Color;
                    archivedSubject.IsActive = true;

                    await _context.SaveChangesAsync();

                    var restoredDTO = _mapper.Map<SubjectReadDto>(archivedSubject);
                    return CreatedAtAction(nameof(GetSubject), new { id = archivedSubject.Id }, restoredDTO);
                }

                // Check for duplicate active subject with same combination
                var duplicateExists = await _context.Subjects
                    .AnyAsync(s =>
                        s.SubjectCode != null &&
                        s.SubjectCode.ToUpper() == code.ToUpper() &&
                        s.YearLevel == dto.YearLevel &&
                        s.CollegeCourseId == dto.CollegeCourseId &&
                        s.IsActive);

                if (duplicateExists)
                    return BadRequest($"A subject with code '{code}' already exists for {dto.YearLevel} in this course.");

                // Create new subject
                var subject = _mapper.Map<Subject>(dto);
                subject.SubjectCode = code;
                subject.IsActive = true;

                _context.Subjects.Add(subject);
                await _context.SaveChangesAsync();

                var subjectDTO = _mapper.Map<SubjectReadDto>(subject);
                return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subjectDTO);
            }
            catch (DbUpdateException ex)
            {
                // Handle database unique constraint violations
                if (ex.InnerException?.Message.Contains("IX_Subjects_SubjectCode") == true)
                {
                    return BadRequest($"A subject with code '{dto.SubjectCode}' already exists. Please use a different subject code.");
                }
                else if (ex.InnerException?.Message.Contains("duplicate key") == true)
                {
                    return BadRequest("A subject with this combination already exists. Please check your input and try again.");
                }

                // Re-throw if it's a different database error
                throw;
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubject(int id, SubjectUpdateDto dto)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null || !subject.IsActive)
                return NotFound("Subject not found or inactive.");

            // Normalize incoming code
            var newCode = dto.SubjectCode?.Trim();
            if (string.IsNullOrWhiteSpace(newCode))
                return BadRequest("SubjectCode is required.");

            // Check for duplicate combination (excluding current subject)
            var duplicateExists = await _context.Subjects
                .AnyAsync(s =>
                    s.Id != id &&
                    s.SubjectCode != null &&
                    s.SubjectCode.ToUpper() == newCode.ToUpper() &&
                    s.YearLevel == dto.YearLevel &&
                    s.CollegeCourseId == dto.CollegeCourseId &&
                    s.IsActive);

            if (duplicateExists)
                return BadRequest($"A subject with code '{newCode}' already exists for {dto.YearLevel} in this course. Please use a different code or modify the existing subject.");

            // Map properties (AutoMapper or manual)
            _mapper.Map(dto, subject);

            // Ensure trimmed code is saved
            subject.SubjectCode = newCode;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Subject/5 (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null || !subject.IsActive)
                return NotFound("Subject not found or already inactive.");

            subject.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Subject/archived
        // Returns all soft-deleted subjects
        [HttpGet("archived")]
        public async Task<ActionResult<IEnumerable<SubjectReadDto>>> GetArchivedSubjects()
        {
            var archived = await _context.Subjects
                .Where(s => !s.IsActive)
                .Include(s => s.CollegeCourse)
                .ToListAsync();

            var dtos = _mapper.Map<List<SubjectReadDto>>(archived);
            return Ok(dtos);
        }

        // PUT: api/Subject/{id}/restore
        // Reactivates a soft-deleted subject
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> RestoreSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound("Subject not found.");

            if (subject.IsActive)
                return BadRequest("Subject is already active.");

            // Check if restoring would create a duplicate
            var duplicateExists = await _context.Subjects
                .AnyAsync(s =>
                    s.Id != id &&
                    s.SubjectCode != null &&
                    s.SubjectCode.ToUpper() == subject.SubjectCode.ToUpper() &&
                    s.YearLevel == subject.YearLevel &&
                    s.CollegeCourseId == subject.CollegeCourseId &&
                    s.IsActive);

            if (duplicateExists)
                return BadRequest($"Cannot restore: A subject with code '{subject.SubjectCode}' already exists for {subject.YearLevel} in this course. Please archive or modify the existing subject first.");

            subject.IsActive = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}