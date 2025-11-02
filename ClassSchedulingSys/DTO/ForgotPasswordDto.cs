namespace ClassSchedulingSys.DTO
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty; // URL-encoded token from email link
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
