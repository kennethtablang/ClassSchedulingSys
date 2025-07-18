﻿// ClassSchedulingSys/Controllers/SubjectController
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
                return NotFound();

            return _mapper.Map<SubjectReadDto>(subject);
        }

        // POST: api/Subject
        [HttpPost]
        public async Task<ActionResult<SubjectReadDto>> CreateSubject(SubjectCreateDto dto)
        {
            var subject = _mapper.Map<Subject>(dto);
            subject.IsActive = true;

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            var subjectDTO = _mapper.Map<SubjectReadDto>(subject);
            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, subjectDTO);
        }

        // PUT: api/Subject/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubject(int id, SubjectUpdateDto dto)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null || !subject.IsActive)
                return NotFound();

            _mapper.Map(dto, subject);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Subject/5 (Soft Delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null || !subject.IsActive)
                return NotFound();

            subject.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 🔹 GET: api/Subject/archived
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

        // 🔹 PUT: api/Subject/{id}/restore
        // Reactivates a soft-deleted subject
        [HttpPut("{id}/restore")]
        public async Task<IActionResult> RestoreSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null)
                return NotFound();

            if (subject.IsActive)
                return BadRequest("Subject is already active.");

            subject.IsActive = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
