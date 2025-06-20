using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

            var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
            var result = await _userMgr.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Optionally assign default role:
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

            return Ok(new { Token = token });
        }
    }
}
