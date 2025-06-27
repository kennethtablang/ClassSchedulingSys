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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public FacultyController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Get all users with the "Faculty" role
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFacultyUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var facultyList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Faculty"))
                {
                    facultyList.Add(new
                    {
                        user.Id,
                        user.FirstName,
                        user.MiddleName,
                        user.LastName,
                        user.Email,
                        user.PhoneNumber,
                        user.DepartmentId,
                        IsActive = user.IsActive,
                        Roles = roles
                    });
                }
            }

            return Ok(facultyList);
        }
    }
}
