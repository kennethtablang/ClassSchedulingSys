﻿namespace ClassSchedulingSys.DTO
{
    public class AdminResetPasswordDto
    {
        public string UserId { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}
