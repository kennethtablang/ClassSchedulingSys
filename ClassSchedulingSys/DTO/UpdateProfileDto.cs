namespace ClassSchedulingSys.DTO
{
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = null!;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public bool? IsActive { get; set; }
    }
}
