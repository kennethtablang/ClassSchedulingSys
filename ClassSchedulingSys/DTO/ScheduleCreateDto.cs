using System;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.DTO
{
    public class ScheduleCreateDto
    {
        [Required]
        public int SubjectId { get; set; }

        [Required]
        public DayOfWeek Day { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public string FacultyId { get; set; }

        [Required]
        public int ClassSectionId { get; set; }
    }

    public class ScheduleUpdateDto
    {
        [Required]
        public int SubjectId { get; set; }

        [Required]
        public DayOfWeek Day { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public string FacultyId { get; set; }

        [Required]
        public int ClassSectionId { get; set; }
    }

    public class ScheduleReadDto
    {
        public int Id { get; set; }

        public DayOfWeek Day { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        // Subject Info
        public int SubjectId { get; set; }
        public string SubjectTitle { get; set; } = string.Empty;
        public string SubjectColor { get; set; } = "#000000"; // default black
        public int SubjectUnits { get; set; }

        // Room Info
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;

        // Faculty Info
        public string FacultyId { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;

        // Class Section Info
        public int ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;
    }
}
