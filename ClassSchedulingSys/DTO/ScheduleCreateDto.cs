using System;
using System.ComponentModel.DataAnnotations;

namespace ClassSchedulingSys.DTO
{
    public class ScheduleCreateDto
    {
        public DayOfWeek Day { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string FacultyId { get; set; } = string.Empty;

        public int RoomId { get; set; }

        public int SubjectId { get; set; }

        public int ClassSectionId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class ScheduleUpdateDto
    {
        public int Id { get; set; }

        public DayOfWeek Day { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string FacultyId { get; set; } = string.Empty;

        public int RoomId { get; set; }

        public int SubjectId { get; set; }

        public int ClassSectionId { get; set; }

        public bool IsActive { get; set; }
    }

    public class ScheduleReadDto
    {
        public int Id { get; set; }

        public DayOfWeek Day { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public double Duration { get; set; }

        public string FacultyId { get; set; }
        public string FacultyName { get; set; } = string.Empty;

        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;

        public int SubjectId { get; set; }
        public string SubjectTitle { get; set; } = string.Empty;
        public int SubjectUnits { get; set; }
        public string SubjectColor { get; set; } = "#999999";

        public int ClassSectionId { get; set; }
        public string ClassSectionName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
