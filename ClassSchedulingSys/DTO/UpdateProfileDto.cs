// ClassSchedulingSys/DTO/UpdateProfileDto.cs
namespace ClassSchedulingSys.DTO
{
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }

        public string? EmployeeID { get; set; }
    }

    public class ChangeEmailRequestDto
    {
        public string NewEmail { get; set; } = string.Empty;
    }

    public class ConfirmChangeEmailDto
    {
        public string UserId { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class RequestEmailChangeDto
    {
        public string NewEmail { get; set; } = string.Empty;
    }

    public class ConfirmEmailChangeDto
    {
        public string NewEmail { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
