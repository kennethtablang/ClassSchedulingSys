using ClassSchedulingSys.Data;
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
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FacultyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/faculty
        [HttpGet]
        public async Task<IActionResult> GetFaculty()
        {
            var faculty = await _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Faculty")))
                .Include(u => u.Department)
                .ToListAsync();

            return Ok(faculty);
        }

        // GET: api/faculty/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFacultyById(string id)
        {
            var faculty = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (faculty == null)
                return NotFound();

            return Ok(faculty);
        }

        // POST: api/faculty
        [HttpPost]
        public async Task<IActionResult> CreateFaculty([FromBody] ApplicationUser faculty)
        {
            var result = await _userManager.CreateAsync(faculty, "DefaultPass123!");
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(faculty, "Faculty");
            return CreatedAtAction(nameof(GetFacultyById), new { id = faculty.Id }, faculty);
        }

        // PUT: api/faculty/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFaculty(string id, [FromBody] ApplicationUser updatedFaculty)
        {
            if (id != updatedFaculty.Id)
                return BadRequest("ID mismatch");

            var faculty = await _context.Users.FindAsync(id);
            if (faculty == null)
                return NotFound();

            faculty.FirstName = updatedFaculty.FirstName;
            faculty.MiddleName = updatedFaculty.MiddleName;
            faculty.LastName = updatedFaculty.LastName;
            faculty.Email = updatedFaculty.Email;
            faculty.UserName = updatedFaculty.UserName;
            faculty.DepartmentId = updatedFaculty.DepartmentId;

            _context.Users.Update(faculty);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/faculty/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFaculty(string id)
        {
            var faculty = await _context.Users.FindAsync(id);
            if (faculty == null)
                return NotFound();

            await _userManager.DeleteAsync(faculty);
            return Ok(new { Message = $"Faculty {id} deleted successfully." });
        }
    }
}
