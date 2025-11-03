// ClassSchedulingSys/Controllers/AuthController.cs
using ClassSchedulingSys.DTO;
using ClassSchedulingSys.Helpers;
using ClassSchedulingSys.Interfaces;
using ClassSchedulingSys.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace ClassSchedulingSys.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly SignInManager<ApplicationUser> _signInMgr;
        private readonly ITokenService _tokenSvc;
        private readonly IEmailService _emailSvc;
        private readonly IConfiguration _config;
        private readonly string _twoFaSecret; // used for hashing
        private readonly TimeSpan _twoFaExpiry = TimeSpan.FromMinutes(5);
        private readonly int _twoFaMaxAttempts = 5;

        public AuthController(UserManager<ApplicationUser> userMgr, SignInManager<ApplicationUser> signInMgr, ITokenService tokenSvc, IEmailService emailSvc, IConfiguration config)
        {
            _userMgr = userMgr;
            _signInMgr = signInMgr;
            _tokenSvc = tokenSvc;
            _emailSvc = emailSvc;
            _config = config;
            _twoFaSecret = config["TwoFactor:Secret"] ?? "change_this_in_production";
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

            // Normalize employee id
            var empId = string.IsNullOrWhiteSpace(dto.EmployeeID) ? null : dto.EmployeeID.Trim();

            // Pre-check: is EmployeeID already used?
            if (!string.IsNullOrWhiteSpace(empId))
            {
                var empExists = await _userMgr.Users
                    .AnyAsync(u => u.EmployeeID != null && u.EmployeeID.ToUpper() == empId.ToUpper());

                if (empExists)
                    return BadRequest("EmployeeID already taken.");
            }

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                MiddleName = dto.MiddleName,
                LastName = dto.LastName,
                EmployeeID = empId,

                // IMPORTANT: set to pending approval
                IsActive = false,     // cannot log in until approved
                IsApproved = false,   // admin must approve
                EmailConfirmed = false // admin will confirm upon approval
            };

            IdentityResult result;
            try
            {
                result = await _userMgr.CreateAsync(user, dto.Password);
            }
            catch (DbUpdateException dbEx)
            {
                // Handle DB unique constraint race (defensive)
                if (dbEx.InnerException is SqlException sqlEx &&
                    (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    // Unique constraint violation (likely EmployeeID)
                    return BadRequest("EmployeeID already taken.");
                }

                throw; // rethrow other DB exceptions
            }
            // notify admins (simplest approach: email all users in Admin role)
            var admins = await _userMgr.GetUsersInRoleAsync("SuperAdmin");
            var adminEmails = admins.Select(a => a.Email).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            var adminBody = $@"
            <p>A new faculty registration needs approval:</p>
            <ul>
                <li>Name: {user.FullName}</li>
                <li>Email: {user.Email}</li>
                <li>EmployeeID: {user.EmployeeID}</li>
            </ul>
            <p>Please login to the admin panel to approve or deny this user.</p>";

            foreach (var email in adminEmails)
            {
                try { await _emailSvc.SendEmailAsync(email!, "New registration awaiting approval", adminBody); }
                catch { /* log */ }
            }


            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // assign Faculty role so admin can manage them
            await _userMgr.AddToRoleAsync(user, "Faculty");

            // Optionally notify admins that there is a new request (you can implement later).
            // For now, return a message to the registrant.
            return Ok("Registration submitted — waiting for admin approval.");
        }



        // ClassSchedulingSys/Controllers/AuthController.cs - Updated Login Method
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userMgr.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid credentials.");

            // Block deactivated users
            if (!user.IsActive)
            {
                return Unauthorized("Account is deactivated.");
            }

            // Block users who haven't been approved by admin yet
            if (!user.IsApproved)
            {
                return Unauthorized("Account pending approval. Please wait for an administrator to approve your account.");
            }

            // ✅ NEW: Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                return Unauthorized(new
                {
                    Message = "Email not confirmed. Please check your email for the confirmation link.",
                    RequiresEmailConfirmation = true,
                    UserId = user.Id
                });
            }

            var signInResult = await _signInMgr.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!signInResult.Succeeded)
                return Unauthorized("Invalid credentials.");

            // If 2FA is enabled for this user and email is confirmed -> initiate 2FA flow
            if (user.TwoFactorEnabled && user.EmailConfirmed)
            {
                // generate numeric code
                var code = TwoFactorHelper.GenerateNumericCode(6);
                // compute hash
                var hash = TwoFactorHelper.ComputeHash(code, _twoFaSecret);
                user.TwoFactorCodeHash = hash;
                user.TwoFactorCodeExpiry = DateTime.UtcNow.Add(_twoFaExpiry);
                user.TwoFactorAttempts = 0;
                var updateResult = await _userMgr.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return StatusCode(500, "Failed to set 2FA code.");

                // send email (simple HTML)
                var body = $"<p>Your login code is <strong>{code}</strong>. It expires in {_twoFaExpiry.TotalMinutes} minutes.</p>";
                await _emailSvc.SendEmailAsync(user.Email!, "Your ClassSchedulingSys login code", body);

                return Ok(new TwoFactorInitiateResultDto
                {
                    RequiresTwoFactor = true,
                    UserId = user.Id,
                    Message = "A verification code has been sent to your email."
                });
            }

            // If no 2FA -> create token and return it
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
                EmployeeID = user.EmployeeID,
                Roles = roles
            });
        }

        // Add these endpoints to your ClassSchedulingSys/Controllers/AuthController.cs

        /// <summary>
        /// Toggle 2FA on or off for the current authenticated user
        /// </summary>
        [HttpPost("toggle-2fa")]
        [Authorize]
        public async Task<IActionResult> Toggle2FA([FromBody] Toggle2FADto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            // Check if email is confirmed before enabling 2FA
            if (dto.Enabled && !user.EmailConfirmed)
                return BadRequest("Email must be confirmed before enabling 2FA.");

            user.TwoFactorEnabled = dto.Enabled;
            var result = await _userMgr.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest("Failed to update 2FA settings.");

            return Ok(new
            {
                Message = dto.Enabled ? "2FA enabled successfully." : "2FA disabled successfully.",
                TwoFactorEnabled = user.TwoFactorEnabled
            });
        }

        /// <summary>
        /// Get current user's 2FA status
        /// </summary>
        [HttpGet("2fa-status")]
        [Authorize]
        public async Task<IActionResult> Get2FAStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            return Ok(new
            {
                TwoFactorEnabled = user.TwoFactorEnabled,
                EmailConfirmed = user.EmailConfirmed
            });
        }

        // Add this DTO class to ClassSchedulingSys/DTO/ folder
        public class Toggle2FADto
        {
            public bool Enabled { get; set; }
        }



        [HttpPost("confirm-2fa")]
        public async Task<IActionResult> ConfirmTwoFactor([FromBody] ConfirmTwoFactorDto dto)
        {
            var user = await _userMgr.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound("User not found.");

            if (!user.IsActive)
                return Unauthorized("Account is deactivated.");

            if (!user.IsApproved)
                return Unauthorized("Account pending approval.");

            // check expiration
            if (!user.TwoFactorCodeExpiry.HasValue || user.TwoFactorExpiryUtcExpired())
            {
                // helper below checks expiry; but fallback:
                if (!user.TwoFactorCodeExpiry.HasValue || user.TwoFactorCodeExpiry.Value < DateTime.UtcNow)
                    return BadRequest("The code has expired. Please request a new one.");
            }

            // check attempt limit
            if (user.TwoFactorAttempts >= _twoFaMaxAttempts)
            {
                // clear code to require new one
                user.TwoFactorCodeHash = null;
                user.TwoFactorCodeExpiry = null;
                user.TwoFactorAttempts = 0;
                await _userMgr.UpdateAsync(user);
                return BadRequest("Too many failed attempts. A new code is required.");
            }

            // validate code
            var providedHash = TwoFactorHelper.ComputeHash(dto.Code, _twoFaSecret);
            if (user.TwoFactorCodeHash == null || !string.Equals(user.TwoFactorCodeHash, providedHash, StringComparison.Ordinal))
            {
                user.TwoFactorAttempts += 1;
                await _userMgr.UpdateAsync(user);
                return BadRequest("Invalid code.");
            }

            // success: clear saved code and issue JWT
            user.TwoFactorCodeHash = null;
            user.TwoFactorCodeExpiry = null;
            user.TwoFactorAttempts = 0;
            await _userMgr.UpdateAsync(user);

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
                EmployeeID = user.EmployeeID,
                Roles = roles
            });
        }

        [HttpPost("resend-2fa")]
        public async Task<IActionResult> ResendTwoFactor([FromBody] ResendTwoFactorDto dto)
        {
            var user = await _userMgr.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound("User not found.");

            if (!user.IsActive)
                return Unauthorized("Account is deactivated.");

            if (!user.IsApproved)
                return Unauthorized("Account pending approval.");

            if (!user.EmailConfirmed) return BadRequest("Email not confirmed.");

            // Rate limiting: simple approach - disallow if still valid for >1 minute
            if (user.TwoFactorCodeExpiry.HasValue && user.TwoFactorCodeExpiry.Value > DateTime.UtcNow.AddMinutes(-4))
            {
                return BadRequest("A code was recently sent. Please wait a moment before requesting again.");
            }

            var code = TwoFactorHelper.GenerateNumericCode(6);
            var hash = TwoFactorHelper.ComputeHash(code, _twoFaSecret);
            user.TwoFactorCodeHash = hash;
            user.TwoFactorCodeExpiry = DateTime.UtcNow.Add(_twoFaExpiry);
            user.TwoFactorAttempts = 0;
            await _userMgr.UpdateAsync(user);

            var body = $"<p>Your new login code is <strong>{code}</strong>. It expires in {_twoFaExpiry.TotalMinutes} minutes.</p>";
            await _emailSvc.SendEmailAsync(user.Email!, "Your ClassSchedulingSys login code (resend)", body);

            return Ok(new TwoFactorInitiateResultDto { RequiresTwoFactor = true, UserId = user.Id, Message = "New verification code sent." });
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
                user.DepartmentId,
                EmployeeID = user.EmployeeID,
                TwoFactorEnabled = user.TwoFactorEnabled,  // ✅ Added
                EmailConfirmed = user.EmailConfirmed        // ✅ Added
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

            // ✅ Normalize incoming EmployeeID (optional)
            var newEmpId = string.IsNullOrWhiteSpace(dto.EmployeeID) ? null : dto.EmployeeID.Trim();

            // ✅ If EmployeeID is changing, pre-check uniqueness
            if (!string.Equals(user.EmployeeID ?? "", newEmpId ?? "", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(newEmpId))
                {
                    var empExists = await _userMgr.Users
                        .AnyAsync(u => u.Id != userId && u.EmployeeID != null && u.EmployeeID.ToUpper() == newEmpId.ToUpper());

                    if (empExists)
                        return BadRequest("EmployeeID already taken.");
                }
            }

            user.FirstName = dto.FirstName;
            user.MiddleName = dto.MiddleName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;
            user.EmployeeID = newEmpId; // ✅ Apply the new or null value

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

        /// <summary>
        /// Initiates a password reset — sends a reset link to the given email if it exists.
        /// Always returns 200 to avoid revealing whether the email exists.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Email))
                return BadRequest("Email is required.");

            // Try to find the user by email. We won't reveal result to caller.
            var user = await _userMgr.FindByEmailAsync(dto.Email);

            if (user != null)
            {
                // Optional: check if user's email is confirmed. You may still want to allow reset even if not confirmed.
                // if (!user.EmailConfirmed) { /* optionally skip sending */ }

                // Generate token
                var token = await _userMgr.GeneratePasswordResetTokenAsync(user);

                // URL-encode the token so it is safe in query string
                var encodedToken = Uri.EscapeDataString(token);

                // Build confirmation link. Prefer a ClientUrl configured in appsettings (frontend route)
                var clientUrl = _config["ClientUrl"]?.TrimEnd('/');
                string callbackUrl;
                if (!string.IsNullOrWhiteSpace(clientUrl))
                {
                    // Frontend should handle route '/reset-password' and accept userId & token
                    callbackUrl = $"{clientUrl}/reset-password?userId={user.Id}&token={encodedToken}";
                }
                else
                {
                    // Fallback to API endpoint which can accept token and return a message (not typical)
                    callbackUrl = Url.Action(nameof(ResetPassword), "Auth", new { userId = user.Id, token = encodedToken }, Request.Scheme) ??
                                  $"{Request.Scheme}://{Request.Host}/api/auth/reset-password?userId={user.Id}&token={encodedToken}";
                }

                // Email body (simple HTML)
                var body = $@"
                    <p>Dear {user.FullName},</p>
                    <p>You requested a password reset for your ClassSchedulingSys account. Click the link below to reset your password:</p>
                    <p><a href=""{callbackUrl}"">Reset my password</a></p>
                    <p>If the link doesn't work, copy/paste the following URL into your browser:</p>
                    <p>{callbackUrl}</p>
                    <p>If you didn't request this, you can safely ignore this email.</p>
                    <p>— PCNL ClassSchedulingSys</p>";

                // Enqueue/send email. If sending fails, we do not reveal that to the requester.
                try
                {
                    await _emailSvc.SendEmailAsync(user.Email!, "Reset your ClassSchedulingSys password", body);
                }
                catch
                {
                    // optionally log error with ILogger — don't expose details to caller
                }
            }

            // Always return same message to avoid account enumeration
            return Ok(new { Message = "If an account with that email exists, a password reset link has been sent." });
        }

        /// <summary>
        /// Completes the password reset using the token emailed to the user.
        /// Expects a POST from frontend with userId, token, new password.
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (dto == null) return BadRequest("Invalid request.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                return BadRequest("Password and confirmation are required.");

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest("Passwords do not match.");

            if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest("Missing userId or token.");

            var user = await _userMgr.FindByIdAsync(dto.UserId);
            if (user == null)
                return BadRequest("Invalid token or user.");

            // ✅ Decode token (since it comes URL-encoded from the email link)
            var decodedToken = Uri.UnescapeDataString(dto.Token);

            var result = await _userMgr.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest($"Could not reset password: {errors}");
            }

            return Ok("Password has been reset successfully. You may now log in with your new password.");
        }

        // Add these methods to ClassSchedulingSys/Controllers/AuthController.cs

        /// <summary>
        /// Request initial email confirmation (for newly approved users)
        /// Generates and sends a 6-digit OTP to user's email
        /// </summary>
        [HttpPost("request-email-confirmation")]
        [Authorize]
        public async Task<IActionResult> RequestEmailConfirmation()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            // If already confirmed, no need to send
            if (user.EmailConfirmed)
                return BadRequest("Email already confirmed.");

            // Generate 6-digit code
            var code = TwoFactorHelper.GenerateNumericCode(6);
            var hash = TwoFactorHelper.ComputeHash(code, _twoFaSecret);

            // Store in TwoFactorCodeHash temporarily (reusing the field)
            user.TwoFactorCodeHash = hash;
            user.TwoFactorCodeExpiry = DateTime.UtcNow.Add(_twoFaExpiry);
            user.TwoFactorAttempts = 0;

            var updateResult = await _userMgr.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return StatusCode(500, "Failed to generate confirmation code.");

            // Send email
            var body = $@"
                <p>Dear {user.FullName},</p>
                <p>Welcome to the PCNL Class Scheduling System!</p>
                <p>Your email confirmation code is: <strong style='font-size: 24px; color: #2563eb;'>{code}</strong></p>
                <p>This code expires in {_twoFaExpiry.TotalMinutes} minutes.</p>
                <p>If you didn't request this code, please contact your system administrator.</p>
                <p>— PCNL ClassSchedulingSys</p>";

            try
            {
                await _emailSvc.SendEmailAsync(user.Email!, "Confirm Your Email - Class Scheduling System", body);
                return Ok(new { Message = "Confirmation code sent to your email." });
            }
            catch (Exception ex)
            {
                // Log error but don't expose details
                return StatusCode(500, "Failed to send confirmation email.");
            }
        }

        /// <summary>
        /// Confirm email with OTP code
        /// </summary>
        [HttpPost("confirm-email")]
        [Authorize]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            if (user.EmailConfirmed)
                return BadRequest("Email already confirmed.");

            // Check expiration
            if (!user.TwoFactorCodeExpiry.HasValue || user.TwoFactorCodeExpiry.Value < DateTime.UtcNow)
                return BadRequest("The code has expired. Please request a new one.");

            // Check attempt limit
            if (user.TwoFactorAttempts >= _twoFaMaxAttempts)
            {
                user.TwoFactorCodeHash = null;
                user.TwoFactorCodeExpiry = null;
                user.TwoFactorAttempts = 0;
                await _userMgr.UpdateAsync(user);
                return BadRequest("Too many failed attempts. Please request a new code.");
            }

            // Validate code
            var providedHash = TwoFactorHelper.ComputeHash(dto.Code, _twoFaSecret);
            if (user.TwoFactorCodeHash == null || !string.Equals(user.TwoFactorCodeHash, providedHash, StringComparison.Ordinal))
            {
                user.TwoFactorAttempts += 1;
                await _userMgr.UpdateAsync(user);
                return BadRequest("Invalid code.");
            }

            // Success: confirm email and clear code
            user.EmailConfirmed = true;
            user.TwoFactorCodeHash = null;
            user.TwoFactorCodeExpiry = null;
            user.TwoFactorAttempts = 0;

            var updateResult = await _userMgr.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return StatusCode(500, "Failed to confirm email.");

            return Ok(new { Message = "Email confirmed successfully!" });
        }

        /// <summary>
        /// Resend email confirmation code
        /// </summary>
        [HttpPost("resend-email-confirmation")]
        [Authorize]
        public async Task<IActionResult> ResendEmailConfirmation()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _userMgr.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            if (user.EmailConfirmed)
                return BadRequest("Email already confirmed.");

            // Rate limiting: disallow if code was sent recently
            if (user.TwoFactorCodeExpiry.HasValue &&
                user.TwoFactorCodeExpiry.Value > DateTime.UtcNow.AddMinutes(-1))
            {
                return BadRequest("A code was recently sent. Please wait before requesting again.");
            }

            // Generate new code
            var code = TwoFactorHelper.GenerateNumericCode(6);
            var hash = TwoFactorHelper.ComputeHash(code, _twoFaSecret);

            user.TwoFactorCodeHash = hash;
            user.TwoFactorCodeExpiry = DateTime.UtcNow.Add(_twoFaExpiry);
            user.TwoFactorAttempts = 0;

            await _userMgr.UpdateAsync(user);

            // Send email
            var body = $@"
                <p>Dear {user.FullName},</p>
                <p>Your new email confirmation code is: <strong style='font-size: 24px; color: #2563eb;'>{code}</strong></p>
                <p>This code expires in {_twoFaExpiry.TotalMinutes} minutes.</p>
                <p>— PCNL ClassSchedulingSys</p>";

            await _emailSvc.SendEmailAsync(user.Email!, "Confirm Your Email - Class Scheduling System", body);

            return Ok(new { Message = "New confirmation code sent to your email." });
        }

        // Add this DTO class
        public class ConfirmEmailDto
        {
            public string Code { get; set; } = null!;
        }


    }

    // Extension to check expiry
    public static class ApplicationUserExtensions
    {
        public static bool TwoFactorExpiryUtcExpired(this ApplicationUser user)
        {
            return !user.TwoFactorCodeExpiry.HasValue || user.TwoFactorCodeExpiry.Value < DateTime.UtcNow;
        }
    }

}
