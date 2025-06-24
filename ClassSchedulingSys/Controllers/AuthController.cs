using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly SignInManager<ApplicationUser> _signInMgr;
        private readonly ITokenService _tokenSvc;

        public AuthController(UserManager<ApplicationUser> userMgr, SignInManager<ApplicationUser> signInMgr, ITokenService tokenSvc)
        {
            _userMgr = userMgr;
            _signInMgr = signInMgr;
            _tokenSvc = tokenSvc;
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            var userExists = await _userMgr.FindByEmailAsync(dto.Email);
            if (userExists != null)
                return BadRequest("Email is already registered.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName
            };

            var result = await _userMgr.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userMgr.AddToRoleAsync(user, "Faculty");

            return Ok("Registration successful.");
        }

        /// <summary>
        /// Logs in an existing user and returns a JWT
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userMgr.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid credentials.");

            var signInResult = await _signInMgr.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!signInResult.Succeeded)
                return Unauthorized("Invalid credentials.");

            var roles = await _userMgr.GetRolesAsync(user);
            var token = _tokenSvc.CreateToken(user, roles);

            return Ok(new
            {
                Token = token,
                user.Id,
                user.Email,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                FullName = user.FullName,
                Roles = roles
            });
        }

        /// <summary>
        /// Gets the profile of the currently authenticated user
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                FullName = user.FullName,
                user.PhoneNumber,
                user.DepartmentId
            });
        }

        /// <summary>
        /// Updates the profile of the currently authenticated user
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.FirstName = dto.FirstName;
            user.MiddleName = dto.MiddleName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _userMgr.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
