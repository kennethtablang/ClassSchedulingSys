namespace ClassSchedulingSys.DTO
{
    public class TwoFactorInitiateResultDto
    {
        public bool RequiresTwoFactor { get; set; }
        public string? UserId { get; set; }
        public string? Message { get; set; }
    }

    public class ConfirmTwoFactorDto
    {
        public string UserId { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

    public class ResendTwoFactorDto
    {
        public string UserId { get; set; } = null!;
    }
}
