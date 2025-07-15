// ClassSchedulingSys/Controllers/UserController
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClassSchedulingSys.Controllers
{
    //This is the Admin Controller to Modify the User Controller
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Faculty,Dean,SuperAdmin")]
    public class UserController : Controller
    {

        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;

        public UserController(UserManager<ApplicationUser> userMgr, RoleManager<IdentityRole> roleMgr)
        {
            _userMgr = userMgr;
            _roleMgr = roleMgr;
        }

        /// <summary>
        /// Get all users with roles
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userMgr.Users.ToListAsync();

            var userList = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userMgr.GetRolesAsync(user);

                userList.Add(new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.FirstName,
                    user.MiddleName,
                    user.LastName,
                    user.PhoneNumber,
                    user.DepartmentId,
                    IsActive = user.IsActive,
                    Roles = roles
                });
            }

            return Ok(userList);
        }

        /// <summary>
        /// Manually add a new user
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> AddUser([FromBody] RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var existing = await _userMgr.FindByEmailAsync(dto.Email);
            if (existing != null)
                return BadRequest("Email already exists.");

            var user = new ApplicationUser
            {
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName
            };

            var result = await _userMgr.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userMgr.AddToRoleAsync(user, "Faculty");

            return Ok("User created successfully.");
        }

        /// <summary>
        /// Update basic user info (not roles)
        /// </summary>
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateProfileDto dto)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound("User not found.");

            user.FirstName = dto.FirstName;
            user.MiddleName = dto.MiddleName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _userMgr.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }

        /// <summary>
        /// Assign roles to user
        /// </summary>
        [HttpPut("users/{id}/roles")]
        public async Task<IActionResult> AssignRole(string id, [FromBody] RoleAssignmentDto dto)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound("User not found.");

            var currentRoles = await _userMgr.GetRolesAsync(user);
            var validRoles = _roleMgr.Roles.Select(r => r.Name).ToList();

            if (!validRoles.Contains(dto.Role))
                return BadRequest("Invalid role.");

            await _userMgr.RemoveFromRolesAsync(user, currentRoles);
            await _userMgr.AddToRoleAsync(user, dto.Role);

            return Ok("Role updated.");
        }

        /// <summary>
        /// Admin resets a user's password
        /// </summary>
        [HttpPut("users/{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordDto dto)
        {
            if (dto.UserId != id)
                return BadRequest("User ID mismatch.");

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var user = await _userMgr.FindByIdAsync(id);
            if (user == null)
                return NotFound("User not found.");

            var token = await _userMgr.GeneratePasswordResetTokenAsync(user);
            var result = await _userMgr.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Password reset successfully.");
        }


        /// <summary>
        /// Toggle a user's active/deactivated status
        /// </summary>
        [HttpPatch("users/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound("User not found.");

            user.IsActive = !user.IsActive;
            var result = await _userMgr.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest("Failed to update user status.");

            return Ok(new
            {
                user.Id,
                user.Email,
                user.IsActive,
                Message = user.IsActive ? "User activated." : "User deactivated."
            });
        }

        /// <summary>
        /// Get all deactivated users (archived)
        /// </summary>
        [HttpGet("users/archived")]
        public async Task<IActionResult> GetArchivedUsers()
        {
            var users = await _userMgr.Users
                .Where(u => !u.IsActive)
                .ToListAsync();

            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userMgr.GetRolesAsync(user);
                result.Add(new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.FirstName,
                    user.MiddleName,
                    user.LastName,
                    user.PhoneNumber,
                    user.DepartmentId,
                    IsActive = user.IsActive,
                    Roles = roles
                });
            }

            return Ok(result);
        }
    }
}
