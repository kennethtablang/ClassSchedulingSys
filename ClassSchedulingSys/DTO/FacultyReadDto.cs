namespace ClassSchedulingSys.DTO
{
    public class FacultyReadDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; }
    }
}
