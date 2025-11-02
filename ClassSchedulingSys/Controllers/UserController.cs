// ClassSchedulingSys/Controllers/UserController
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Web;

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
        private readonly IEmailService _emailSvc;
        private readonly IConfiguration _config;

        public UserController(UserManager<ApplicationUser> userMgr, RoleManager<IdentityRole> roleMgr, IEmailService emailSvc, IConfiguration config)
        {
            _userMgr = userMgr;
            _roleMgr = roleMgr;
            _emailSvc = emailSvc;
            _config = config;
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
                    EmployeeID = user.EmployeeID,
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

            var empId = string.IsNullOrWhiteSpace(dto.EmployeeID) ? null : dto.EmployeeID.Trim();

            // Pre-check EmployeeID
            if (!string.IsNullOrWhiteSpace(empId))
            {
                var empExists = await _userMgr.Users
                    .AnyAsync(u => u.EmployeeID != null && u.EmployeeID.ToUpper() == empId.ToUpper());

                if (empExists)
                    return BadRequest("EmployeeID already taken.");
            }

            var user = new ApplicationUser
            {
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                EmployeeID = empId
            };

            IdentityResult result;
            try
            {
                result = await _userMgr.CreateAsync(user, dto.Password); // note: _user_mgr should be _userMgr in your actual file
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is SqlException sqlEx &&
                    (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    return BadRequest("EmployeeID already taken.");
                }
                throw;
            }

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

            // Normalize incoming EmployeeID (optional)
            var newEmpId = string.IsNullOrWhiteSpace(dto.EmployeeID) ? null : dto.EmployeeID.Trim();

            // If EmployeeID is changing, pre-check uniqueness
            if (!string.Equals(user.EmployeeID ?? "", newEmpId ?? "", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(newEmpId))
                {
                    var empExists = await _userMgr.Users
                        .AnyAsync(u => u.Id != id && u.EmployeeID != null && u.EmployeeID.ToUpper() == newEmpId.ToUpper());

                    if (empExists)
                        return BadRequest("EmployeeID already taken.");
                }
            }

            user.FirstName = dto.FirstName;
            user.MiddleName = dto.MiddleName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.EmployeeID = newEmpId; // either new value or null

            IdentityResult result;
            try
            {
                result = await _userMgr.UpdateAsync(user);
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is SqlException sqlEx &&
                    (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    return BadRequest("EmployeeID already taken.");
                }
                throw;
            }

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }

        // POST: api/user/profile/change-email
        [HttpPost("profile/change-email")]
        [Authorize] // must be signed-in user
        public async Task<IActionResult> RequestEmailChange([FromBody] ChangeEmailRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.NewEmail))
                return BadRequest("New email is required.");

            var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized();

            var user = await _userMgr.FindByIdAsync(currentUserId);
            if (user == null) return NotFound("User not found.");

            // If same as current, nothing to do
            if (string.Equals(user.Email, dto.NewEmail, StringComparison.OrdinalIgnoreCase))
                return BadRequest("This is already your registered email.");

            // Check email not already used by another account
            var existing = await _userMgr.FindByEmailAsync(dto.NewEmail);
            if (existing != null)
                return BadRequest("This email is already in use. Please use a different email.");

            // Generate change-email token (Identity)
            var token = await _userMgr.GenerateChangeEmailTokenAsync(user, dto.NewEmail);

            // Create confirmation link. We'll use current request host as fallback. If you have a front-end URL in config, you can redirect there.
            var encodedToken = Uri.EscapeDataString(token);
            var confirmEndpoint = Url.Action(
                action: nameof(ConfirmChangeEmailDto),
                controller: "User",
                values: new { userId = user.Id, email = dto.NewEmail, token = encodedToken },
                protocol: Request.Scheme);

            // Fallback if Url.Action returned null (rare)
            if (string.IsNullOrWhiteSpace(confirmEndpoint))
            {
                confirmEndpoint = $"{Request.Scheme}://{Request.Host}/api/user/confirm-change-email?userId={user.Id}&email={Uri.EscapeDataString(dto.NewEmail)}&token={encodedToken}";
            }

            // Email body with instructions and note to ignore if not requested
            var body = $@"
                <p>Dear {user.FullName},</p>
                <p>You (or someone using your session) requested to change your ClassSchedulingSys account email to <strong>{HttpUtility.HtmlEncode(dto.NewEmail)}</strong>.</p>
                <p>To confirm this change, please click the link below (or copy and paste it into your browser). This link expires according to system policy.</p>
                <p><a href=""{confirmEndpoint}"">Confirm my new email address</a></p>
                <p>If you did not request this change, you can safely ignore this message — no changes will be made.</p>
                <p>If you do not receive this email, please try another active email address.</p>
                <p>— PCNL ClassSchedulingSys</p>";

            try
            {
                await _emailSvc.SendEmailAsync(dto.NewEmail, "Confirm your new email — Class Scheduling System", body);
            }
            catch (Exception ex)
            {
                // Log if you have a logger (not shown). Return a friendly message.
                return StatusCode(500, "Failed to send confirmation email. Please try another email or contact the administrator.");
            }

            return Ok(new { Message = $"A confirmation email has been sent to {dto.NewEmail}. Please check your inbox (or spam)." });
        }

        // GET: api/user/confirm-change-email?userId=...&email=...&token=...
        [HttpGet("confirm-change-email")]
        [AllowAnonymous] // token protects the operation
        public async Task<IActionResult> ConfirmChangeEmail([FromQuery] string userId, [FromQuery] string email, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return BadRequest("Missing parameters.");

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // The token was URL-encoded; ensure it's decoded
            var decodedToken = Uri.UnescapeDataString(token);

            // Attempt to change email using the token
            var changeResult = await _userMgr.ChangeEmailAsync(user, email, decodedToken);
            if (!changeResult.Succeeded)
            {
                // Combine identity errors into a single message
                var errors = string.Join("; ", changeResult.Errors.Select(e => e.Description));
                return BadRequest($"Could not confirm email change: {errors}");
            }

            // Also update UserName to the new email so login via email works (if your system uses UserName==Email)
            user.UserName = email;
            user.EmailConfirmed = true; // confirm the new email immediately
            var updateResult = await _userMgr.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return StatusCode(500, $"Email changed but failed to update user record: {errors}");
            }

            // Optional: redirect to a frontend confirmation page if CLIENT_URL is configured
            var clientUrl = _config["ClientUrl"]; // set e.g. in appsettings or env var
            if (!string.IsNullOrWhiteSpace(clientUrl))
            {
                // append a success route, e.g. /email-confirmed
                var redirect = clientUrl.TrimEnd('/') + "/email-confirmed";
                return Redirect(redirect);
            }

            return Ok("Email updated and confirmed. You can now log in using your new email.");
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
                    EmployeeID = user.EmployeeID,
                    IsActive = user.IsActive,
                    Roles = roles
                });
            }

            return Ok(result);
        }

        // GET: api/user/pending-approvals
        [HttpGet("pending-approvals")]
        [Authorize(Roles = "Dean,SuperAdmin")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var pending = await _userMgr.Users
                .Where(u => !u.IsApproved)
                .ToListAsync();

            var result = new List<object>();
            foreach (var u in pending)
            {
                var roles = await _userMgr.GetRolesAsync(u);
                result.Add(new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    FullName = u.FullName,
                    u.EmployeeID,
                    Roles = roles,
                    u.IsActive,
                    u.IsApproved
                });
            }

            return Ok(result);
        }

        // PUT: api/user/{id}/approve
        [HttpPut("users/{id}/approve")]
        [Authorize(Roles = "Dean,SuperAdmin")]
        public async Task<IActionResult> ApproveUser(string id)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound("User not found.");

            // Mark approved + active + confirm email (so they can login)
            user.IsApproved = true;
            user.IsActive = true;
            user.EmailConfirmed = true;
            user.ApprovalMessage = null; // clear any previous message

            var updateResult = await _userMgr.UpdateAsync(user);
            if (!updateResult.Succeeded) return BadRequest(updateResult.Errors);

            // send approval email
            var body = $@"
                <p>Dear {user.FullName},</p>
                <p>Your account for <strong>Class Scheduling System</strong> has been <strong>approved</strong> by the admin.</p>
                <p>You can now sign in using the email <strong>{user.Email}</strong>.</p>
                <p><strong>Instructions:</strong></p>
                <ol>
                    <li>Go to the system login page.</li>
                    <li>Enter your registered email and password.</li>
                    <li>If Two-Factor Authentication is enabled, check your email for the verification code.</li>
                    <li>When logged in, update your profile and set up 2FA preferences if needed.</li>
                </ol>
                <p>If you need help, reply to this message or contact your administrator.</p>
                <p>— PCNL Class Scheduling System</p>";

            try
            {
                await _emailSvc.SendEmailAsync(user.Email!, "Account approved — Class Scheduling System", body);
            }
            catch
            {
                // log if you have a logger; do not block approval if email fails
            }

            return Ok(new { Message = "User approved and notified." });
        }

        // PUT: api/user/{id}/deny
        [HttpPut("users/{id}/deny")]
        [Authorize(Roles = "Dean,SuperAdmin")]
        public async Task<IActionResult> DenyUser(string id, [FromBody] string? reason)
        {
            var user = await _userMgr.FindByIdAsync(id);
            if (user == null) return NotFound("User not found.");

            // Mark as denied (leave as inactive)
            user.IsApproved = false;
            user.IsActive = false;
            user.EmailConfirmed = false;
            user.ApprovalMessage = reason;

            var updateResult = await _userMgr.UpdateAsync(user);
            if (!updateResult.Succeeded) return BadRequest(updateResult.Errors);

            // send denial email
            var body = $@"
                <p>Dear {user.FullName},</p>
                <p>We regret to inform you that your account registration for <strong>Class Scheduling System</strong> has been <strong>denied</strong>.</p>
                {(string.IsNullOrWhiteSpace(reason) ? "" : $"<p><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(reason)}</p>")}
                <p>If you believe this is a mistake or you want to request re-evaluation, please contact the administration.</p>
                <p>— PCNL Class Scheduling System</p>";

            try
            {
                await _emailSvc.SendEmailAsync(user.Email!, "Account registration denied — Class Scheduling System", body);
            }
            catch
            {
                // log if you have a logger; do not block
            }

            return Ok(new { Message = "User denied and notified." });
        }

    }
}
