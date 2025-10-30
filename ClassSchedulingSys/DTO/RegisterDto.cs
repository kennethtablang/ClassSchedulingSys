// ClassSchedulingSys/DTO/RegisterDto
namespace ClassSchedulingSys.DTO
{
    public class RegisterDto
    {
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; } // Optional
        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;

        public string? EmployeeID { get; set; }
    }
}
