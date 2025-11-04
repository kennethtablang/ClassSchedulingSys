// ClassSchedulingSys/DTO/FacultyAvailabilityDto.cs
namespace ClassSchedulingSys.DTO
{
    public class FacultyAvailabilityDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? EmployeeID { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class FacultyScheduleAvailabilityDto
    {
        public string FacultyId { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string Day { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public List<ConflictingScheduleDto> ConflictingSchedules { get; set; } = new();
    }

    public class ConflictingScheduleDto
    {
        public int ScheduleId { get; set; }
        public string SubjectTitle { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }
}