﻿using ClassSchedulingSys.Data;
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Dean,SuperAdmin")]
    public class ClassSectionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassSectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/classsection
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClassSectionDto>>> GetAll()
        {
            var sections = await _context.ClassSections
                .Include(cs => cs.CollegeCourse)
                .Include(cs => cs.Semester)
                    .ThenInclude(s => s.SchoolYear)
                .Select(cs => new ClassSectionDto
                {
                    Id = cs.Id,
                    Section = cs.Section,
                    YearLevel = cs.YearLevel,
                    CollegeCourseId = cs.CollegeCourseId,
                    CollegeCourseCode = cs.CollegeCourse.Code,
                    CollegeCourseName = cs.CollegeCourse.Name,
                    SemesterId = cs.SemesterId,
                    SemesterName = cs.Semester.Name,
                    SchoolYearLabel = cs.Semester.SchoolYear != null
                        ? $"{cs.Semester.SchoolYear.StartYear}-{cs.Semester.SchoolYear.EndYear}"
                        : string.Empty
                })
                .ToListAsync();

            return Ok(sections);
        }

        // GET: api/classsection/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ClassSectionDto>> GetById(int id)
        {
            var cs = await _context.ClassSections
                .Include(cs => cs.CollegeCourse)
                .Include(cs => cs.Semester)
                    .ThenInclude(s => s.SchoolYear)
                .FirstOrDefaultAsync(cs => cs.Id == id);

            if (cs == null) return NotFound();

            var dto = new ClassSectionDto
            {
                Id = cs.Id,
                Section = cs.Section,
                YearLevel = cs.YearLevel,
                CollegeCourseId = cs.CollegeCourseId,
                CollegeCourseCode = cs.CollegeCourse.Code,
                CollegeCourseName = cs.CollegeCourse.Name,
                SemesterId = cs.SemesterId,
                SemesterName = cs.Semester.Name,
                SchoolYearLabel = cs.Semester.SchoolYear != null
                    ? $"{cs.Semester.SchoolYear.StartYear}-{cs.Semester.SchoolYear.EndYear}"
                    : string.Empty
            };

            return Ok(dto);
        }

        // POST: api/classsection
        [HttpPost]
        public async Task<IActionResult> Create(CreateClassSectionDto dto)
        {
            var section = new ClassSection
            {
                Section = dto.Section,
                YearLevel = dto.YearLevel,
                CollegeCourseId = dto.CollegeCourseId,
                SemesterId = dto.SemesterId
            };

            _context.ClassSections.Add(section);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Class section created successfully." });
        }

        // PUT: api/classsection/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateClassSectionDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch.");

            var section = await _context.ClassSections.FindAsync(id);
            if (section == null) return NotFound();

            section.Section = dto.Section;
            section.YearLevel = dto.YearLevel;
            section.CollegeCourseId = dto.CollegeCourseId;
            section.SemesterId = dto.SemesterId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Class section updated successfully." });
        }

        // DELETE: api/classsection/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var section = await _context.ClassSections.FindAsync(id);
            if (section == null) return NotFound();

            _context.ClassSections.Remove(section);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Class section deleted." });
        }

    }
}
